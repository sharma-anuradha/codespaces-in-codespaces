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
    /// <remarks>
    /// When making changes in this class take a look at \src\Codespaces\EnvironmentManager\Src\EnvironmentManager\Tasks\WatchOrphanedSystemEnvironmentsTask.cs.
    /// </remarks>
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
                    await TaskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_run_unit_check",
                        idShards,
                        (idShard, itemLogger) => CoreRunUnitAsync(idShard, claimSpan, itemLogger),
                        childLogger,
                        (idShard, itemLogger) => ObtainLease($"{LeaseBaseName}-{idShard}", claimSpan, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
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

            var cutoffTime = DateTime.UtcNow.AddDays(-7);

            // Get record so we can tell if it exists
            // Note: On the filter - we select records based on the filter below.
            // We don't want to clean up resources which are early in the stage of creation in this worker. This can happen either it is assigned or not. Look at queued mode.
            // If already assigned. Whether or not we have AzureResourceAlive doesn't matter, since it was already assigned, we want this cleaned up.
            // If not assigned, but AzureResourceAlive wasn't updated in a while.
            return ResourceRepository.ForEachAsync(
                x => x.KeepAlives != default &&
                     x.Id.StartsWith(idShard) &&
                     x.Created < cutoffTime &&
                     (x.Type == ResourceType.ComputeVM || x.Type == ResourceType.OSDisk || x.Type == ResourceType.StorageArchive || x.Type == ResourceType.StorageFileShare) &&
                     ((x.IsAssigned && x.Assigned < cutoffTime && (x.KeepAlives.EnvironmentAlive == default || x.KeepAlives.EnvironmentAlive < cutoffTime)) ||
                      (!x.IsAssigned && (x.KeepAlives.AzureResourceAlive == default || x.KeepAlives.AzureResourceAlive < cutoffTime))),
                logger.NewChildLogger(),
                (resource, innerLogger) =>
                {
                    innerLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id);

                    // Log each item
                    return innerLogger.OperationScopeAsync(
                        $"{LogBaseName}_process_record",
                        async (childLogger)
                        =>
                        {
                            childLogger
                                .FluentAddValue("ResourceCutoffTime", cutoffTime)
                                .FluentAddValue("ResourceIsAssigned", resource.IsAssigned)
                                .FluentAddValue("ResourceAssigned", resource.Assigned)
                                .FluentAddValue("ResourceEnvironmentAliveDate", resource.KeepAlives?.EnvironmentAlive)
                                .FluentAddValue("ResourceAzureResourceAliveDate", resource.KeepAlives?.AzureResourceAlive)
                                .FluentAddValue("ResourceCreatedDate", resource.Created);

                            // Trigger delete
                            // await childLogger.OperationScopeAsync(
                            //     $"{LogBaseName}_delete_record",
                            //     (deleteLogger) => DeleteResourceAsync(resource.Id, deleteLogger));
                            childLogger.LogError($"{LogBaseName}_orphaned_resource_detected");

                            // Pause to rate limit ourselves
                            await Task.Delay(QueryDelay);
                        });
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        private async Task DeleteResourceAsync(string id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, "OrphanedSystemResource");

            // Since we don't have the azyre resource, we are just goig to delete this record
            await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
