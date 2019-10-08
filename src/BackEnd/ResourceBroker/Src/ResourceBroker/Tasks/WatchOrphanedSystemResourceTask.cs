// <copyright file="WatchOrphanedSystemResourceTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned Azure Resource.
    /// </summary>
    public class WatchOrphanedSystemResourceTask : IWatchOrphanedSystemResourceTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedSystemResourceTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchOrphanedSystemResourceTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceRepository = resourceRepository;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedSystemResourceTask)}Lease");

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedSystemResourceTask;

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IResourceRepository ResourceRepository { get; }

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
                    await TaskHelper.RunBackgroundEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, claimSpan, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLease($"{LeaseBaseName}-{idShard}", claimSpan, logger));

                    return !Disposed;
                },
                (e) => !Disposed);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task CoreRunUnitAsync(string idShard, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskResourceIdShard", idShard);

            // Executes the action that needs to be performed on the pool
            await logger.TrackDurationAsync(
                "RunPoolAction",
                () =>
                {
                    var cutoffTime = DateTime.UtcNow.AddDays(-1);

                    // Get record so we can tell if it exists
                    return ResourceRepository.ForEachAsync(
                        x => x.KeepAlives != null
                            && x.KeepAlives.AzureResourceAlive < cutoffTime,
                        logger.NewChildLogger(),
                        (resource, innerLogger) =>
                        {
                            // Log each item 
                            return innerLogger.OperationScopeAsync(
                                $"{LogBaseName}_process_record",
                                async (childLogger)
                                =>
                                {
                                    // Capture consistency of keep alives
                                    var keepAlivesAreConsistent = resource.KeepAlives?.EnvironmentAlive < cutoffTime;

                                    childLogger.FluentAddBaseValue("ResourceId", resource.Id)
                                        .FluentAddValue("ResourceEnvironmentAliveDate", resource.KeepAlives?.EnvironmentAlive)
                                        .FluentAddValue("ResourceAzureResourceAliveDate", resource.KeepAlives?.AzureResourceAlive)
                                        .FluentAddValue("ResourceKeepAlivesAreConsistent", keepAlivesAreConsistent);

                                    // Only remove if keep alives are consistent
                                    if (keepAlivesAreConsistent)
                                    {
                                        // Trigger delete
                                        await childLogger.OperationScopeAsync(
                                            $"{LogBaseName}_delete_record",
                                            (deleteLogger) => DeleteResourceAsync(resource.Id, deleteLogger));

                                        // Pause to rate limit ourselves
                                        await Task.Delay(QueryDelay);
                                    }
                                });
                        },
                        (_, __) => Task.Delay(QueryDelay));
                });
        }

        private async Task DeleteResourceAsync(string id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("OperationReason", "OrphanedSystemResource");

            // Since we don't have the azyre resource, we are just goignt to delete this record
            await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
