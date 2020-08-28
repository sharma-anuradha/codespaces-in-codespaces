// <copyright file="WatchEnvironmentsToBeArchivedTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
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
    /// Watch Suspended Environments Task to be Archived.
    /// </summary>
    public class WatchEnvironmentsToBeArchivedTask : EnvironmentTaskBase, IWatchEnvironmentsToBeArchivedTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentsToBeArchivedTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="environmentContinuationOperations">Target Resource Broker Http Client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchEnvironmentsToBeArchivedTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentContinuationOperations environmentContinuationOperations,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IConfigurationReader configurationReader)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder, configurationReader)
        {
            EnvironmentContinuationOperations = environmentContinuationOperations;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchEnvironmentsToBeArchivedTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchEnvironmentsToBeArchivedTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchSuspendedEnvironmentsToBeArchivedTask;

        private IEnvironmentContinuationOperations EnvironmentContinuationOperations { get; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Bail if disabled
                    var isEnabled = await EnvironmentManagerSettings.EnvironmentArchiveEnabled(childLogger);
                    childLogger.FluentAddValue("TaskIsEnabled", isEnabled);
                    if (!isEnabled)
                    {
                        return !Disposed;
                    }

                    // Settings for query
                    var cutoffHoursForShutdown = await EnvironmentManagerSettings.SuspendedEnvironmentArchiveCutoffHours(childLogger);
                    var cutoffHoursForSoftDeleted = await EnvironmentManagerSettings.SoftDeletedEnvironmentArchiveCutoffHours(childLogger);
                    var cutoffTimeForShutdown = DateTime.UtcNow.AddHours(cutoffHoursForShutdown * -1);
                    var cutoffTimeForSoftDeleted = DateTime.UtcNow.AddHours(cutoffHoursForSoftDeleted * -1);
                    var batchSize = await EnvironmentManagerSettings.EnvironmentArchiveBatchSize(childLogger);
                    var maxActiveCount = await EnvironmentManagerSettings.EnvironmentArchiveMaxActiveCount(childLogger);

                    childLogger.FluentAddValue("TaskEnvironmentCutoffHoursForShutdown", cutoffHoursForShutdown)
                        .FluentAddValue("TaskEnvironmentCutoffTimeForShutdown", cutoffTimeForShutdown)
                        .FluentAddValue("TaskEnvironmentCutoffHoursForSoftDelete", cutoffHoursForSoftDeleted)
                        .FluentAddValue("TaskEnvironmentCutoffTimeForSoftDelete", cutoffTimeForSoftDeleted)
                        .FluentAddValue("TaskEnvironmentMaxActiveCount", maxActiveCount)
                        .FluentAddValue("TaskRequestedItems", batchSize);

                    var idShards = ScheduledTaskHelpers.GetIdShards();

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, cutoffTimeForShutdown, cutoffTimeForSoftDeleted, batchSize, maxActiveCount, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string idShard, DateTime cutoffTimeForShutdown, DateTime cutoffTimeForSoftDeleted, int batchSize, int maxActiveCount, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            // Check to see how many jobs are currently running
            var activeCount = await CloudEnvironmentRepository.GetEnvironmentsArchiveJobActiveCountAsync(logger.NewChildLogger());

            logger.FluentAddValue("TaskEnvironmentActiveCount", activeCount);

            // Check that we aren't over limit
            if (activeCount <= maxActiveCount)
            {
                // Set batchsize for what we need
                batchSize = Math.Min(batchSize, maxActiveCount - activeCount);

                logger.FluentAddValue("TaskdRequestedItemsAdjuste", batchSize);

                // Get environments to be archived
                var records = await CloudEnvironmentRepository.GetEnvironmentsReadyForArchiveAsync(
                    idShard, batchSize, cutoffTimeForShutdown, cutoffTimeForSoftDeleted, logger.NewChildLogger());

                logger.FluentAddValue("TaskFoundItems", records.Count());

                // Run through each found item
                foreach (var record in records)
                {
                    await CoreRunUnitAsync(record, logger);
                }
            }
        }

        private Task CoreRunUnitAsync(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_archive",
                async (childLogger) =>
                {
                    // TODO: Need to add filtering so that we only target speicfic subscriptions
                    //       to start, plus add feature flag, etc
                    // App config on and off, then system config from database
                    childLogger.FluentAddBaseValue("EnvironmentId", environment.Id)
                        .FluentAddBaseValue("ArchiveAttemptCount", environment.Transitions.Archiving.AttemptCount);

                    // Trigger the environment archive continuation task.
                    await EnvironmentContinuationOperations.ArchiveAsync(
                        Guid.Parse(environment.Id),
                        environment.LastStateUpdated,
                        "EnvironmentArchiveTimeoutHit",
                        childLogger.NewChildLogger());

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                },
                swallowException: true);
        }
    }
}
