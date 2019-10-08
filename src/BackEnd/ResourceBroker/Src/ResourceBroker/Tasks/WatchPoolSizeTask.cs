// <copyright file="WatchPoolSizeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that watches the pool size and determins if any delta operations need to be
    /// performed to fill/drain the pool.
    /// </summary>
    public class WatchPoolSizeTask : BaseWatchPoolTask, IWatchPoolSizeTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolSizeTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target reesource broker settings.</param>
        /// <param name="resourcePoolManager">Target resource pool manager.</param>
        /// <param name="continuationTaskActivator">Target continuation activator.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource Repository.</param>
        /// <param name="claimedDistributedLease">Target distributed lease.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="resourceNameBuilder">Target resource name builder.</param>
        public WatchPoolSizeTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourcePoolManager resourcePoolManager,
            IResourceRepository resourceRepository,
            IContinuationTaskActivator continuationTaskActivator,
            IResourcePoolDefinitionStore resourceScalingStore,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder)
            : base(resourceBrokerSettings, resourceScalingStore, claimedDistributedLease, taskHelper, resourceNameBuilder)
        {
            ResourcePoolManager = resourcePoolManager;
            ResourceRepository = resourceRepository;
            ContinuationTaskActivator = continuationTaskActivator;
        }

        /// <inheritdoc/>
        protected override string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchPoolSizeTask)}Lease");

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolSizeTask;

        private IResourcePoolManager ResourcePoolManager { get; }

        private IResourceRepository ResourceRepository { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        /// <inheritdoc/>
        protected async override Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            // Determine the effective size of the pool
            var unassignedCount = await GetPoolUnassignedCountAsync(resourcePool, logger.NewChildLogger());

            // Determine if the pool is currently enabled
            var poolEnabled = ResourcePoolManager.IsPoolEnabled(resourcePool.Details.GetPoolDefinition());

            logger.FluentAddValue("PoolIsEnabled", poolEnabled);

            // Short circuit things if we have a fail and drain the pool
            if (!poolEnabled)
            {
                logger.FluentAddValue("PoolDrainCount", unassignedCount);

                // Get the ids of the items in the pool so that we drain them off
                var unassignedIds = await GetPoolUnassignedAsync(resourcePool, unassignedCount, logger);

                logger.FluentAddValue("PoolDrainCountFound", unassignedIds.Count());

                // Delete each of the items that are not current
                foreach (var unassignedId in unassignedIds)
                {
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_run_delete",
                        (childLogger) => DeletePoolItemAsync(Guid.Parse(unassignedId), childLogger),
                        logger);
                }
            }
            else
            {
                // Get the desiered pool target size
                var poolTargetCount = resourcePool.TargetCount;

                // Get the delta of how many
                var poolDeltaCount = poolTargetCount - unassignedCount;

                logger.FluentAddValue("SizeCheckUnassignedCount", unassignedCount.ToString())
                    .FluentAddValue("SizeCheckPoolTargetCount", poolTargetCount.ToString())
                    .FluentAddValue("SizeCheckPoolDeltaCount", poolDeltaCount.ToString());

                // If we have any positive delta add that many jobs to the queue for processing
                if (poolDeltaCount > 0)
                {
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
                    // Get some items we can delete
                    var unassignedIds = await GetPoolUnassignedAsync(resourcePool, poolDeltaCount * -1, logger);

                    logger.FluentAddValue("PoolDrainCountFound", unassignedIds.Count());

                    // Delete each of the items that are not current
                    foreach (var unassignedId in unassignedIds)
                    {
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_run_delete",
                            (childLogger) => DeletePoolItemAsync(Guid.Parse(unassignedId), childLogger),
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

            logger.FluentAddBaseValue("TaskJobIteration", iteration.ToString())
                .FluentAddBaseValue("ResourceId", id);

            await ContinuationTaskActivator.CreateResource(
                id, resourcePool.Type, resourcePool.Details, "WatchPoolSizeIncrease", logger.NewChildLogger());
        }

        private async Task DeletePoolItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", id);

            await ContinuationTaskActivator.DeleteResource(id, "WatchPoolSizeDecrease", logger.NewChildLogger());
        }
    }
}
