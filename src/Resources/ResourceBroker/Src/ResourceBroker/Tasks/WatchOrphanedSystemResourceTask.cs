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
using Microsoft.VsSaaS.Services.CloudEnvironments.Scheduler;

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
        /// <param name="resourcePoolDefinitionStore">Target resource pool definition store.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchOrphanedSystemResourceTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceRepository = resourceRepository;
            ResourcePoolDefinitionStore = resourcePoolDefinitionStore;
            TaskHelper = taskHelper;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedSystemResourceTask)}Lease");

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedSystemResourceTask;

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

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
                    var idShards = ScheduledTaskHelpers.GetIdShards();

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

            var flagCutoffTime = DateTime.UtcNow.AddDays(-1);
            var deleteCutoffTime = DateTime.UtcNow.AddDays(-7);

            // Get record so we can tell if it exists
            // Note: On the filter - we select records based on the filter below.
            // We don't want to clean up resources which are early in the stage of creation in this worker. This can happen either it is assigned or not. Look at queued mode.
            // If already assigned. Whether or not we have AzureResourceAlive doesn't matter, since it was already assigned, we want this cleaned up.
            // If not assigned, but AzureResourceAlive wasn't updated in a while.
            return ResourceRepository.ForEachAsync(
                x => x.KeepAlives != default &&
                     x.Id.StartsWith(idShard) &&
                     x.Created < flagCutoffTime &&
                     (x.Type == ResourceType.ComputeVM || x.Type == ResourceType.OSDisk || x.Type == ResourceType.StorageArchive || x.Type == ResourceType.StorageFileShare) &&
                     ((x.IsAssigned && x.Assigned < flagCutoffTime && (x.KeepAlives.EnvironmentAlive == default || x.KeepAlives.EnvironmentAlive < flagCutoffTime)) ||
                      (!x.IsAssigned && (x.KeepAlives.AzureResourceAlive == default || x.KeepAlives.AzureResourceAlive < flagCutoffTime))),
                logger.NewChildLogger(),
                (resource, innerLogger) =>
                {
                    // Log each item
                    return innerLogger.OperationScopeAsync(
                        $"{LogBaseName}_process_record",
                        async (childLogger)
                        =>
                        {
                            // Determine if we are over the delete cutoff time
                            var timeToDeletion = resource.Created - deleteCutoffTime;
                            var shouldDelete = resource.Created < deleteCutoffTime;

                            // Take care of logging
                            childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id)
                                .FluentAddValue("ResourceCutoffTime", flagCutoffTime)
                                .FluentAddValue("ResourceIsAssigned", resource.IsAssigned)
                                .FluentAddValue("ResourceAssigned", resource.Assigned)
                                .FluentAddValue("ResourceEnvironmentAliveDate", resource.KeepAlives?.EnvironmentAlive)
                                .FluentAddValue("ResourceAzureResourceAliveDate", resource.KeepAlives?.AzureResourceAlive)
                                .FluentAddValue("ResourceCreatedDate", resource.Created)
                                .FluentAddValue(ResourceLoggingPropertyConstants.PoolResourceType, resource.Type)
                                .FluentAddValue("ResourceProvisioningStatus", resource.ProvisioningStatus)
                                .FluentAddValue("ResourceProvisioningReason", resource.ProvisioningReason)
                                .FluentAddValue("ResourceStartingStatus", resource.StartingStatus)
                                .FluentAddValue("ResourceStartingReason", resource.StartingReason)
                                .FluentAddValue("ResourceDeletingStatus", resource.DeletingStatus)
                                .FluentAddValue("ResourceDeletingReason", resource.DeletingReason)
                                .FluentAddValue("ResourceCleanupStatus", resource.CleanupStatus)
                                .FluentAddValue("ResourceCleanupReason", resource.CleanupReason)
                                .FluentAddValue("OrphanShouldDelete", shouldDelete)
                                .FluentAddValue("OrphanDaysToDeletion", shouldDelete ? 0 : timeToDeletion.TotalDays);

                            var poolDefinition = await ResourcePoolDefinitionStore.MapPoolCodeToResourceSku(resource.PoolReference.Code);
                            if (poolDefinition != null)
                            {
                                childLogger.FluentAddValue(ResourceLoggingPropertyConstants.PoolSkuName, poolDefinition.Details.SkuName);
                            }

                            // Delete if needed
                            if (shouldDelete)
                            {
                                // Trigger delete
                                // await childLogger.OperationScopeAsync(
                                //     $"{LogBaseName}_delete_record",
                                //     (deleteLogger) => DeleteResourceAsync(resource.Id, deleteLogger));
                                childLogger.NewChildLogger().LogError($"{LogBaseName}_orphaned_resource_deleted");
                            }
                            else
                            {
                                childLogger.NewChildLogger().LogWarning($"{LogBaseName}_orphaned_resource_detected");
                            }

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
