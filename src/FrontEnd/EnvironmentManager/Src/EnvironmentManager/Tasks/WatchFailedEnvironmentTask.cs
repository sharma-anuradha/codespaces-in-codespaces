// <copyright file="WatchFailedEnvironmentTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks
{
    /// <summary>
    /// Watch Failed Environment Task.
    /// </summary>
    public class WatchFailedEnvironmentTask : EnvironmentTaskBase, IWatchFailedEnvironmentTask
    {
        private const int RequestedItems = 10;

        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);
        private static readonly bool IsEnabled = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchFailedEnvironmentTask"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Target Environment Manager Settings.</param>
        /// <param name="cloudEnvironmentRepository">Target Cloud Environment Repository.</param>
        /// <param name="environmentContinuationOperations">Target Resource Broker Http Client.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchFailedEnvironmentTask(
            EnvironmentManagerSettings environmentManagerSettings,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
            : base(environmentManagerSettings, cloudEnvironmentRepository, taskHelper, claimedDistributedLease, resourceNameBuilder)
        {
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchFailedEnvironmentTask)}Lease");

        private string LogBaseName => EnvironmentLoggingConstants.WatchFailedEnvironmentTask;

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            // Hard coded switch to enable and disable archive worker
            if (!IsEnabled)
            {
                return Task.FromResult(!Disposed);
            }

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
            var records = await CloudEnvironmentRepository.GetFailedOperationAsync(
                idShard, RequestedItems, logger.NewChildLogger());

            logger.FluentAddValue("TaskRequestedItems", RequestedItems)
                .FluentAddValue("TaskFoundItems", records.Count());

            foreach (var record in records)
            {
                await CoreRunUnitAsync(record, logger);
            }
        }

        private Task CoreRunUnitAsync(CloudEnvironment record, IDiagnosticsLogger loogger)
        {
            return loogger.OperationScopeAsync(
                $"{LogBaseName}_run_fail_cleanup",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("TaskFailedItemRunId", Guid.NewGuid())
                        .AddEnvironmentId(record.Id);

                    // Record the reason why this one is being deleted
                    var didFailStatus = false;
                    if (record.Transitions.Archiving.Status == OperationState.Failed
                        || record.Transitions.Archiving.Status == OperationState.Cancelled)
                    {
                        didFailStatus = true;
                    }

                    childLogger.FluentAddValue("TaskFailedStatusItem", didFailStatus)
                        .FluentAddValue("TaskFailedStalledItem", !didFailStatus);

                    // Record which operation it failed on
                    var reason = string.Empty;
                    var didFailArchiving = false;
                    var operationFailedTimeLimit = DateTime.UtcNow.AddHours(-1);
                    if (CheckTransitionState(record.Transitions.Archiving, operationFailedTimeLimit))
                    {
                        didFailArchiving = true;
                        reason = "FailArchiving";
                    }

                    childLogger.FluentAddValue("TaskDidFailArchiving", didFailArchiving)
                        .FluentAddValue("TaskDidFailReason", reason);

                    // Delete assuming we have something to do. Double check that only VMs are being deleted if they failed to start.
                    if (didFailArchiving)
                    {
                        childLogger.FluentAddValue("DeleteAttemptCount", record.Transitions.Archiving.AttemptCount);
                        childLogger.LogWarning($"{LogBaseName}_stale_resource_found");

                        // Clear out the recorded archive state
                        await childLogger.RetryOperationScopeAsync(
                            $"{LogBaseName}_process_record",
                            async (innerLogger)
                            =>
                            {
                                // Fetch fresh record
                                record = await CloudEnvironmentRepository.GetAsync(record.Id, innerLogger.NewChildLogger());

                                // Update core properties
                                record.Transitions.Archiving.AttemptCount++;
                                record.Transitions.Archiving.ResetStatus(false);

                                // Do the actual update
                                await CloudEnvironmentRepository.UpdateAsync(record, innerLogger.NewChildLogger());
                            });
                    }
                    else
                    {
                        throw new InvalidOperationException("Unexpected resource state while attempting to clean up resource.");
                    }
                },
                swallowException: true);
        }

        private bool CheckTransitionState(TransitionState transitionState, DateTime operationFailedTimeLimit)
        {
            return transitionState.Status == OperationState.Failed
                || transitionState.Status == OperationState.Cancelled
                || ((transitionState.Status == OperationState.Initialized
                        || transitionState.Status == OperationState.InProgress)
                    && transitionState.StatusChanged <= operationFailedTimeLimit);
        }
    }
}
