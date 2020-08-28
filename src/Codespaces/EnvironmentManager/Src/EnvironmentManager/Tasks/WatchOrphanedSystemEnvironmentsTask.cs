// <copyright file="WatchOrphanedSystemEnvironmentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Orphaned System Environments Task.
    /// </summary>
    /// <remarks>
    /// When making changes in this class take a look at \src\Resources\ResourceBroker\Src\ResourceBroker\Tasks\WatchOrphanedSystemResourceTask.cs.
    /// </remarks>
    public class WatchOrphanedSystemEnvironmentsTask : EnvironmentTaskBase, IWatchOrphanedSystemEnvironmentsTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedSystemEnvironmentsTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedSystemEnvironmentsTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            ControlPlaneInfo = controlPlaneInfo;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedSystemEnvironmentsTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedSystemEnvironmentsTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchOrphanedSystemEnvironmentsTask;

        private IResourceBrokerResourcesHttpContract ResourceBrokerHttpClient { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    var idShards = ScheduledTaskHelpers.GetIdShards();
                    var currentControlPlaneLocation = ControlPlaneInfo.Stamp.Location;

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, currentControlPlaneLocation, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{currentControlPlaneLocation}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private Task CoreRunUnitAsync(string idShard, AzureLocation controlPlaneLocation, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskResourceIdShard", idShard);

            // Get record so we can tell if it exists
            return CloudEnvironmentRepository.ForEachEnvironmentWithComputeOrStorageAsync(
                controlPlaneLocation,
                idShard,
                logger.NewChildLogger(),
                (environment, innerLogger) =>
                {
                    innerLogger.FluentAddBaseValue("EnvironmentId", environment.Id);

                    // Log each item
                    return innerLogger.OperationScopeAsync(
                        $"{LogBaseName}_process_record",
                        async (childLogger)
                        =>
                        {
                            DateTime updateTime = DateTime.Now;

                            // Conduct check to see if the compute resource exist
                            var hasComputeResource = false;
                            var hasCompute = environment.Compute != null;
                            childLogger.FluentAddValue("EnvironmentComputeRecordPresent", hasCompute);
                            if (hasCompute)
                            {
                                hasComputeResource = await SendEnvironmentKeepAliveAsync(environment, environment.Compute, childLogger);
                            }

                            childLogger.FluentAddValue("EnvironmentComputeResourceRecordPresent", hasComputeResource);

                            // Conduct check to see if the storage resource exist
                            var hasStorageResource = false;
                            var hasStorageRecord = environment.Storage != null;
                            childLogger.FluentAddValue("EnvironmentStorageRecordPresent", hasStorageRecord);
                            if (hasStorageRecord)
                            {
                                hasStorageResource = await SendEnvironmentKeepAliveAsync(environment, environment.Storage, childLogger);
                            }

                            childLogger.FluentAddValue("EnvironmentStorageResourceRecordPresent", hasStorageResource);

                            // Conduct check to see if the storage resource exist
                            var hasOSDiskResource = false;
                            var hasOSDiskRecord = environment.OSDisk != null;
                            childLogger.FluentAddValue("EnvironmentOSDiskRecordPresent", hasOSDiskRecord);
                            if (hasOSDiskRecord)
                            {
                                hasOSDiskResource = await SendEnvironmentKeepAliveAsync(environment, environment.OSDisk, childLogger);
                            }

                            childLogger.FluentAddValue("EnvironmentOSDiskResourceRecordPresent", hasOSDiskResource);

                            // Update db to push back keep alive details
                            if (hasComputeResource || hasStorageResource || hasOSDiskResource)
                            {
                                var childLogger2 = childLogger.NewChildLogger();

                                // The top level try/catch is to prevent a single exception from killing rest of the loop.
                                try
                                {
                                    var updatedEnvironmentRecord = await Retry.DoAsync<CloudEnvironment>(
                                    async (int attemptNumber) =>
                                    {
                                        childLogger2.AddAttempt(attemptNumber);

                                        if (hasComputeResource)
                                        {
                                            environment.Compute.KeepAlive.ResourceAlive = updateTime;
                                        }

                                        if (hasStorageResource)
                                        {
                                            environment.Storage.KeepAlive.ResourceAlive = updateTime;
                                        }

                                        if (hasOSDiskResource)
                                        {
                                            environment.OSDisk.KeepAlive.ResourceAlive = updateTime;
                                        }

                                        environment = await CloudEnvironmentRepository.UpdateAsync(environment, childLogger2.NewChildLogger());
                                        return environment;
                                    },
                                    async (attemptNumber, ex) =>
                                    {
                                        childLogger2.AddAttempt(attemptNumber);

                                        if (ex is DocumentClientException dcex && dcex.StatusCode == HttpStatusCode.PreconditionFailed)
                                        {
                                            environment = await CloudEnvironmentRepository.GetAsync(environment.Id, childLogger2.NewChildLogger());
                                        }
                                    });
                                }
                                catch (Exception e)
                                {
                                    childLogger2.LogErrorWithDetail($"{LogBaseName}_update_record_failed", e.ToString());
                                }
                            }

                            // Pause to rate limit ourselves
                            await Task.Delay(QueryDelay);
                        });
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        private async Task<bool> SendEnvironmentKeepAliveAsync(CloudEnvironment environment, ResourceAllocationRecord allocationRecord, IDiagnosticsLogger logger)
        {
            var hasResource = false;
            try
            {
                // Call backend to ensure exists
                hasResource = await ResourceBrokerHttpClient.ProcessHeartbeatAsync(
                    Guid.Parse(environment.Id),
                    allocationRecord.ResourceId,
                    logger.NewChildLogger());
            }
            catch (HttpResponseStatusException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Do nothing
            }

            return hasResource;
        }
    }
}
