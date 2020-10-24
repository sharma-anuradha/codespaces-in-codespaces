// <copyright file="CloudEnvironmentRegionalMigrationTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Migrate cloud environments to their regional control-plane locations.
    /// </summary>
    public class CloudEnvironmentRegionalMigrationTask : EnvironmentTaskBase, ICloudEnvironmentRegionalMigrationTask
    {
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentRegionalMigrationTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="configurationReader">Target configuration reader.</param>
        /// <param name="jobSchedulerFeatureFlags">job queue feature flag</param>
        public CloudEnvironmentRegionalMigrationTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, jobSchedulerFeatureFlags, configurationReader)
        {
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        protected override string ConfigurationBaseName => "CloudEnvironmentRegionalMigrationTask";

        protected override bool IsDeprecated => true;

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(CloudEnvironmentRegionalMigrationTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.CloudEnvironmentRegionalMigrationTask;

        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Basic shard by starting resource id character
                    // NOTE: If over time we needed an additional dimention, we could add region
                    //       and do a cross product with it.
                    var idShards = ScheduledTaskHelpers.GetIdShards();

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string idShard, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            await CloudEnvironmentRepository.GlobalRepository.ForEachAsync(
                (x) => x.Id.StartsWith(idShard) && x.Location == ControlPlaneInfo.Stamp.Location,
                logger.NewChildLogger(),
                (environment, childLogger) =>
                {
                    if (environment.IsMigrated)
                    {
                        return Task.CompletedTask;
                    }

                    return CoreRunUnitAsync(environment, childLogger);
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        private Task CoreRunUnitAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_migrate",
                async (childLogger) =>
                {
                    childLogger.AddEnvironmentId(environment.Id);

                    environment.IsMigrated = true;

                    try
                    {
                        await CloudEnvironmentRepository.RegionalRepository.CreateOrUpdateAsync(environment, logger.NewChildLogger());
                    }
                    catch (DocumentClientException ex)
                    {
                        // Note: If we get a Precondition Failed error, it means that the environment has already been copied to the Regional DB.
                        if (ex.StatusCode != System.Net.HttpStatusCode.PreconditionFailed)
                        {
                            throw;
                        }
                    }

                    await CloudEnvironmentRepository.GlobalRepository.UpdateAsync(environment, logger.NewChildLogger());
                },
                swallowException: true);
        }
    }
}
