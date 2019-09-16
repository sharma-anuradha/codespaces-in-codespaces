// <copyright file="WatchPoolSizeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
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
        /// <param name="continuationTaskActivator">Target continuation activator.</param>
        /// <param name="distributedLease">Target distributed lease.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public WatchPoolSizeTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IContinuationTaskActivator continuationTaskActivator,
            IResourcePoolDefinitionStore resourceScalingStore,
            IDistributedLease distributedLease,
            ITaskHelper taskHelper)
            : base(resourceBrokerSettings, resourceScalingStore, distributedLease, taskHelper)
        {
            ContinuationTaskActivator = continuationTaskActivator;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string LeaseBaseName => "WatchPoolSizeTaskLease";

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolSizeTask;

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected async override Task RunPoolActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            // Determine the effective size of the pool
            var unassignedCount = await GetPoolUnassignedCountAsync(resourcePool, logger.WithValues(new LogValueSet()));

            // Get the desiered pool target size
            var poolTargetCount = resourcePool.TargetCount;

            // Get the delta of how many
            var poolDeltaCount = poolTargetCount - unassignedCount;

            logger.FluentAddValue("CheckUnassignedCount", unassignedCount.ToString())
                .FluentAddValue("CheckPoolTargetCount", poolTargetCount.ToString())
                .FluentAddValue("CheckPoolDeltaCount", poolDeltaCount.ToString());

            // If we have any positive delta add that many jobs to the queue for processing
            if (poolDeltaCount > 0)
            {
                // Add each of the times that we need to have
                for (var i = 0; i < poolDeltaCount; i++)
                {
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_create",
                        (childLogger) => AddPoolItemAsync(resourcePool, i, childLogger),
                        logger);
                }
            }
            else if (poolDeltaCount < 0)
            {
                var unassignedIds = await GetPoolUnassignedAsync(resourcePool, poolDeltaCount * -1, logger);

                // Delete each of the items that are not current
                foreach (var unassignedId in unassignedIds)
                {
                    TaskHelper.RunBackground(
                        $"{LogBaseName}_delete",
                        (childLogger) => DeletetPoolItemAsync(Guid.Parse(unassignedId), childLogger),
                        logger);
                }
            }
        }

        private Task<int> GetPoolUnassignedCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedCountAsync(
                resourcePool.Details.GetPoolDefinition(), logger.WithValues(new LogValueSet()));
        }

        private Task<IEnumerable<string>> GetPoolUnassignedAsync(ResourcePool resourcePool, int count, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedAsync(
                resourcePool.Details.GetPoolDefinition(), count, logger.WithValues(new LogValueSet()));
        }

        private async Task AddPoolItemAsync(ResourcePool resourcePool, int iteration, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ActivityInstanceIterationId", iteration.ToString());

            await ContinuationTaskActivator.CreateResource(
                resourcePool.Type, resourcePool.Details, "WatchPoolSizeIncrease", logger);
        }

        private async Task DeletetPoolItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ActivityInstanceIterationId", id);

            await ContinuationTaskActivator.DeleteResource(Guid.NewGuid(), "WatchPoolSizeDecrease", logger);
        }
    }
}
