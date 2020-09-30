// <copyright file="WatchPoolSizeJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that watches the pool size and determins if any delta operations need to be
    /// performed to fill/drain the pool.
    /// </summary>
    public class WatchPoolSizeJobHandler : WatchPoolJobHandlerBase<WatchPoolSizeJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolSizeJobHandler"/> class.
        /// </summary>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourceContinuationOperations">Target continuation activator.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public WatchPoolSizeJobHandler(
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper)
            : base(resourcePoolDefinitionStore, taskHelper)
        {
            ResourceRepository = resourceRepository;
            ResourceContinuationOperations = resourceContinuationOperations;
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolSizeTask;

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(ResourcePool resourcePool, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Determine the effective size of the pool
            var unassignedCount = await GetPoolUnassignedCountAsync(resourcePool, logger.NewChildLogger());

            logger.FluentAddValue("PoolIsEnabled", resourcePool.IsEnabled)
                .FluentAddValue("PoolOverrideIsEnabled", resourcePool.OverrideIsEnabled)
                .FluentAddValue("PoolTargetCount", resourcePool.TargetCount)
                .FluentAddValue("PoolOverrideTargetCount", resourcePool.OverrideTargetCount);

            // Short circuit things if we have a fail and drain the pool
            if (!resourcePool.IsEnabled)
            {
                logger.FluentAddValue("PoolDrainCount", unassignedCount);

                unassignedCount = Math.Min(unassignedCount, resourcePool.MaxDeleteBatchCount);

                // Get the ids of the items in the pool so that we drain them off
                var unassignedIds = await GetPoolUnassignedAsync(resourcePool, unassignedCount, logger);

                logger.FluentAddValue("PoolDrainCountFound", unassignedIds.Count());

                // Delete each of the items that are not current
                foreach (var unassignedId in unassignedIds)
                {
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_run_delete",
                        (childLogger) => DeletePoolItemAsync(Guid.Parse(unassignedId), "WatchPoolSizePoolDisabled", childLogger),
                        logger);
                }
            }
            else
            {
                // Get the delta of how many
                var poolDeltaCount = resourcePool.TargetCount - unassignedCount;

                logger.FluentAddValue("SizeCheckUnassignedCount", unassignedCount.ToString())
                    .FluentAddValue("SizeCheckPoolDeltaCount", poolDeltaCount.ToString());

                // If we have any positive delta add that many jobs to the queue for processing
                if (poolDeltaCount > 0)
                {
                    // Limits the amount that can be created at any one time
                    poolDeltaCount = Math.Min(poolDeltaCount, resourcePool.MaxCreateBatchCount);

                    // Add each of the times that we need to have
                    for (var i = 0; i < poolDeltaCount; i++)
                    {
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_run_create",
                            (childLogger) => AddPoolItemAsync(resourcePool, i, childLogger),
                            logger);
                    }
                }
                else if (poolDeltaCount < 0)
                {
                    // Limits the amount that can be created at any one time
                    poolDeltaCount = Math.Min(poolDeltaCount * -1, resourcePool.MaxDeleteBatchCount);

                    // Get some items we can delete
                    var unassignedIds = await GetPoolUnassignedAsync(resourcePool, poolDeltaCount, logger);

                    logger.FluentAddValue("PoolDrainCountFound", unassignedIds.Count());

                    // Delete each of the items that are not current
                    foreach (var unassignedId in unassignedIds)
                    {
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_run_delete",
                            (childLogger) => DeletePoolItemAsync(Guid.Parse(unassignedId), "WatchPoolSizeDecrease", childLogger),
                            logger);
                    }
                }
            }
        }

        private Task<int> GetPoolUnassignedCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedCountAsync(
                resourcePool.Details.GetPoolDefinition(), logger.NewChildLogger());
        }

        private Task<IEnumerable<string>> GetPoolUnassignedAsync(ResourcePool resourcePool, int count, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedAsync(
                resourcePool.Details.GetPoolDefinition(), count, logger.NewChildLogger());
        }

        private async Task AddPoolItemAsync(ResourcePool resourcePool, int iteration, IDiagnosticsLogger logger)
        {
            var id = Guid.NewGuid();
            var reason = "WatchPoolSizeIncrease";

            logger.FluentAddBaseValue("TaskJobIteration", iteration.ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            await ResourceContinuationOperations.CreateAsync(
                id, resourcePool.Type, resourcePool.Details, reason, logger.NewChildLogger());
        }

        private async Task DeletePoolItemAsync(Guid id, string reason, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            await ResourceContinuationOperations.DeleteAsync(null, id, reason, logger.NewChildLogger());
        }
    }
}
