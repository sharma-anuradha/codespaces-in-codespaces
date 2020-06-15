// <copyright file="WatchSuspendedEnvironmentsToBeArchivedTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Suspended Environments Task to be Archived.
    /// </summary>
    public class WatchSuspendedEnvironmentsToBeArchivedTask : EnvironmentTaskBase, IWatchSuspendedEnvironmentsToBeArchivedTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchSuspendedEnvironmentsToBeArchivedTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="environmentContinuationOperations">Target Resource Broker Http Client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        public WatchSuspendedEnvironmentsToBeArchivedTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentContinuationOperations environmentContinuationOperations,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder)
        {
            EnvironmentContinuationOperations = environmentContinuationOperations;
            ControlPlaneInfo = controlPlaneInfo;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchSuspendedEnvironmentsToBeArchivedTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchSuspendedEnvironmentsToBeArchivedTask;

        private IEnvironmentContinuationOperations EnvironmentContinuationOperations { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
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
                    var cutoffHours = await EnvironmentManagerSettings.EnvironmentArchiveCutoffHours(childLogger);
                    var cutoffTime = DateTime.UtcNow.AddHours(cutoffHours * -1);
                    var batchSize = await EnvironmentManagerSettings.EnvironmentArchiveBatchSize(childLogger);
                    var maxActiveCount = await EnvironmentManagerSettings.EnvironmentArchiveMaxActiveCount(childLogger);

                    childLogger.FluentAddValue("TaskEnvironmentCutoffHours", cutoffHours)
                        .FluentAddValue("TaskEnvironmentCutoffTime", cutoffTime)
                        .FluentAddValue("TaskEnvironmentMaxActiveCount", maxActiveCount)
                        .FluentAddValue("TaskRequestedItems", batchSize);

                    // Basic shard by starting resource id character
                    // NOTE: If over time we needed an additional dimention, we could add region
                    //       and do a cross product with it.
                    var idShards = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

                    // Run through found resources in the background
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, cutoffTime, batchSize, maxActiveCount, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string idShard, DateTime cutoffTime, int batchSize, int maxActiveCount, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("TaskEnvironmentIdShard", idShard);

            // Pickup current region
            var controlPlaneRegion = ControlPlaneInfo.Stamp.Location;

            // Check to see how many jobs are currently running
            var activeCount = await CloudEnvironmentRepository.GetEnvironmentsArchiveJobActiveCountAsync(
                controlPlaneRegion, logger.NewChildLogger());

            logger.FluentAddValue("TaskEnvironmentActiveCount", activeCount);

            // Check that we aren't over limit
            if (activeCount <= maxActiveCount)
            {
                // Set batchsize for what we need
                batchSize = Math.Min(batchSize, maxActiveCount - activeCount);

                logger.FluentAddValue("TaskdRequestedItemsAdjuste", batchSize);

                // Get environments to be archived
                var records = await CloudEnvironmentRepository.GetEnvironmentsReadyForArchiveAsync(
                    idShard, batchSize, cutoffTime, controlPlaneRegion, logger.NewChildLogger());

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
                        "SuspendedEnvironmentTimeoutHit",
                        childLogger.NewChildLogger());

                    // Pause to rate limit ourselves
                    await Task.Delay(QueryDelay);
                },
                swallowException: true);
        }
    }
}
