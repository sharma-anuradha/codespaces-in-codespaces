// <copyright file="WatchOrphanedSystemEnvironmentsTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Orphaned System Environments Task.
    /// </summary>
    public class WatchOrphanedSystemEnvironmentsTask : IWatchOrphanedSystemEnvironmentsTask
    {
        private const string WatchOrphanedSystemEnvironmentsLeaseContainerName = "watch-orphaned-system-environments-leases";

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
        public WatchOrphanedSystemEnvironmentsTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesHttpContract resourceBrokerHttpClient,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            CloudEnvironmentRepository = cloudEnvironmentRepository;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedSystemEnvironmentsTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchOrphanedSystemEnvironmentsTask;

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerHttpClient { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Basic shard by starting resource id character
                    // NOTE: If over time we needed an additional dimention, we could add region 
                    //       and do a cross product with it.
                    var idShards = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, claimSpan, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLease($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, IDiagnosticsLogger) => !Disposed,
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private Task CoreRunUnitAsync(string idShard, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskResourceIdShard", idShard);

            // Get record so we can tell if it exists
            return CloudEnvironmentRepository.ForEachAsync(
                x => x.Id.StartsWith(idShard)
                    && (x.Storage != null || x.Compute != null),
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
                            // Conduct check to see if the compute resource exist
                            var hasComputeResource = false;
                            var hasCompute = environment.Compute != null;
                            childLogger.FluentAddValue("EnvironmentComputeRecordPresent", hasCompute);
                            if (hasCompute)
                            {
                                try
                                {
                                    // Call backend to ensure exists
                                    hasComputeResource = await ResourceBrokerHttpClient.TriggerEnvironmentHeartbeatAsync(
                                        environment.Compute.ResourceId, childLogger.NewChildLogger());

                                    // Update keep alive details
                                    environment.Compute.KeepAlive.ResourceAlive = DateTime.UtcNow;
                                }
                                catch (HttpResponseStatusException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    // Do nothing
                                }
                            }

                            childLogger.FluentAddValue("EnvironmentComputeResourceRecordPresent", hasComputeResource);

                            // Conduct check to see if the storage resource exist
                            var hasStorageResource = false;
                            var hasStorageRecord = environment.Storage != null;
                            childLogger.FluentAddValue("EnvironmentStorageRecordPresent", hasStorageRecord);
                            if (hasStorageRecord)
                            {
                                try
                                {
                                    hasStorageResource = await ResourceBrokerHttpClient.TriggerEnvironmentHeartbeatAsync(
                                        environment.Storage.ResourceId, childLogger.NewChildLogger());

                                    // Update keep alive details
                                    environment.Storage.KeepAlive.ResourceAlive = DateTime.UtcNow;
                                }
                                catch (HttpResponseStatusException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    // Do nothing
                                }
                            }

                            childLogger.FluentAddValue("EnvironmentStorageResourceRecordPresent", hasStorageResource);

                            // Update db to push back keep alive details
                            if (hasComputeResource || hasStorageResource)
                            {
                                await CloudEnvironmentRepository.UpdateAsync(environment, childLogger.NewChildLogger());
                            }

                            // Pause to rate limit ourselves
                            await Task.Delay(QueryDelay);
                        });
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                EnvironmentManagerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
