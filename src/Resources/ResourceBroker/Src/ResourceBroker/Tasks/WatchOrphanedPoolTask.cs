// <copyright file="WatchOrphanedPoolTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that tries to kick off a continuation which will try and manage tracking
    /// orphaned pools and conduct orchestrate drains as requried.
    /// </summary>
    public class WatchOrphanedPoolTask : BaseBackgroundTask, IWatchOrphanedPoolTask
    {
        // Add an artificial delay between DB queries so that we reduce bursty load on our database to prevent throttling for end users
        private static readonly TimeSpan QueryDelay = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedPoolTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="resourcePoolDefinitionStore">ResourcePoolDefinitionStore info.</param>
        /// <param name="resourceContinuationOperations">ResourceContinuationOperations object to perform the necessary workflows.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedPoolTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IResourceContinuationOperations resourceContinuationOperations,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            ResourceBrokerSettings = Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));
            ResourceRepository = Requires.NotNull(resourceRepository, nameof(resourceRepository));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            ResourcePoolDefinitionStore = Requires.NotNull(resourcePoolDefinitionStore, nameof(resourcePoolDefinitionStore));
            ResourceContinuationOperations = Requires.NotNull(resourceContinuationOperations, nameof(resourceContinuationOperations));
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedPoolTask";

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedPoolTask)}Lease");

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedPoolTask;

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IResourceRepository ResourceRepository { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IResourcePoolDefinitionStore ResourcePoolDefinitionStore { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Fetch distinct list of pool codes
                    var poolCodes = await ResourceRepository.GetPoolCodesForUnassignedAsync(childLogger.NewChildLogger());
                    var poolQueueCodes = await ResourceRepository.GetAllPoolQueueCodesAsync(childLogger.NewChildLogger());

                    var allPoolcodes = new HashSet<string>(poolCodes);
                    
                    // Add pool codes for pools, that do not contain any unassigned resources, but have pool queues. 
                    foreach (var poolQueueCode in poolQueueCodes)
                    {
                        var poolCode = poolQueueCode.GetPoolCodeForQueue();

                        if (!allPoolcodes.Contains(poolCode))
                        {
                            allPoolcodes.Add(poolCode);
                        }
                    }

                    // Fetch active pools in the system
                    var resourcePools = await ResourcePoolDefinitionStore.RetrieveDefinitionsAsync();

                    // Active pools usually wont be empty, if so just skip processing pool records.
                    if (resourcePools.Count() > 0)
                    {
                        // Run through found resources in the background
                        await TaskHelper.RunConcurrentEnumerableAsync(
                            $"{LogBaseName}_run_unit_check",
                            allPoolcodes,
                            async (poolCode, itemLogger) => await CoreRunUnitAsync(poolCode, resourcePools, itemLogger),
                            childLogger,
                            (pool, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{pool}", claimSpan, itemLogger));
                    }

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task CoreRunUnitAsync(string poolReferenceCode, IEnumerable<ResourcePool> resourcePools, IDiagnosticsLogger logger)
        {
            // Determine if the pool is still configured in the system
            var isActive = IsActivePool(poolReferenceCode, resourcePools);

            logger.FluentAddBaseValue("TaskResourcePoolIsToBeDeleted", !isActive);

            if (!isActive)
            {
                await ProcessPoolRecordsAsync(poolReferenceCode, logger);
            }
        }

        /// <summary>
        /// Checks whether the given pool is active or not.
        /// </summary>
        /// <param name="poolReferenceCode">PoolReferenceCode.</param>
        /// <param name="resourcePools">List of resource pools that are active.</param>
        /// <returns>A boolean to know if its active.</returns>
        public bool IsActivePool(string poolReferenceCode, IEnumerable<ResourcePool> resourcePools)
        {
            return resourcePools.Any(pool => pool.Id == poolReferenceCode);
        }

        /// <summary>
        /// Process the records that are for orphaned pools.
        /// </summary>
        /// <param name="poolReferenceCode">PoolReferenceCode.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ProcessPoolRecordsAsync(string poolReferenceCode, IDiagnosticsLogger logger)
        {
            // Queries for resources that
            // 1. belongs to resourcePool and exists and unAssigned.
            // 2. represenrs poolQueue for this resource pool.
            await ResourceRepository.ForEachAsync(
                x => (x.PoolReference.Code == poolReferenceCode &&
                    x.IsAssigned == false &&
                    x.IsDeleted == false) ||
                    x.PoolReference.Code == poolReferenceCode.GetPoolQueueDefinition(),
                logger.NewChildLogger(),
                (resource, innerLogger) =>
                {
                    innerLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id);

                    // Log each item
                    return innerLogger.OperationScopeAsync(
                            $"{LogBaseName}_process_record",
                            async (childLogger) =>
                            {
                                await DeleteResourceAsync(resource.Id, childLogger);
                            });
                },
                (_, __) => Task.Delay(QueryDelay));
        }

        /// <summary>
        /// Deletes the resource with ResourceContinuationOperation,
        /// just marked as delete untill the dependent resources are deleted in Azure.
        /// </summary>
        /// <param name="id">Resource Id.</param>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>A ResourceRecord Id that got deleted.<see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<string> DeleteResourceAsync(string id, IDiagnosticsLogger logger)
        {
            var resourceRecord = await ResourceRepository.GetAsync(id, logger);
            var isAssigned = resourceRecord?.IsAssigned == true;
            var isDeleted = resourceRecord?.IsDeleted == true;
            var isPoolQueue = resourceRecord?.Type == ResourceType.PoolQueue;

            // Delete if resource exists and not assigned or if it is a pool queue resource
            var shouldDelete = (!isAssigned && !isDeleted) || isPoolQueue;
            string deletedResourceId = null;

            logger
                .FluentAddBaseValue("PoolIsAssigned", isAssigned)
                .FluentAddBaseValue("PoolIsDeleted", isDeleted)
                .FluentAddBaseValue("PoolQueue", isPoolQueue)
                .FluentAddBaseValue("PoolShouldDelete", shouldDelete);

            // Double checking to make sure for deletion.
            if (shouldDelete)
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_delete_record",
                    async (innerLogger) =>
                    {
                        var reason = "OrphanedPoolResource";
                        innerLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

                        // Since we don't have this pool's Skuname anymore needed, we are just going to perform a delete for this record
                        await ResourceContinuationOperations.DeleteAsync(null, new Guid(id), reason, logger.NewChildLogger());

                        deletedResourceId = id;
                    });
            }

            return deletedResourceId;
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disposed = true;
        }
    }
}
