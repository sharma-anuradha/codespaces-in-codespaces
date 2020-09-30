// <copyright file="WatchPoolVersionJobHandler.cs" company="Microsoft">
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
    /// Task mananager that tries to tick off a continuation which will try and manage tracking
    /// of the "current" version and conduct orchistrate drains as requried.
    /// </summary>
    public class WatchPoolVersionJobHandler : WatchPoolJobHandlerBase<WatchPoolVersionJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolVersionJobHandler"/> class.
        /// </summary>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourceContinuationOperations">Target continuation activator.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public WatchPoolVersionJobHandler(
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper)
            : base(resourcePoolDefinitionStore, taskHelper)
        {
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolVersionTask;

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(ResourcePool resourcePool, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Check database for non current count
            var unassignedNotVersionCount = await GetPoolUnassignedNotVersionCountAsync(resourcePool, logger);

            logger.FluentAddValue("VersionUnassignedNotVersionCount", unassignedNotVersionCount);

            // If only if we need to do something
            if (unassignedNotVersionCount > 0)
            {
                // See how many are current ready for use in the pool
                var readyUnassignedCount = await GetPoolReadyUnassignedCountAsync(resourcePool, logger);
                var readyUnassignedRate = (double)readyUnassignedCount / resourcePool.TargetCount;
                var dropCount = readyUnassignedCount - (int)(resourcePool.TargetCount * 0.8);

                logger.FluentAddValue("VersionReadyUnassignedCount", readyUnassignedCount)
                    .FluentAddValue("VersionReadyUnassignedRate", readyUnassignedRate)
                    .FluentAddValue("VersionDropCount", dropCount);

                // We only will do anything if the pool has 80% resources ready for actual use
                if (dropCount > 0)
                {
                    dropCount = Math.Min(resourcePool.MaxDeleteBatchCount, dropCount);

                    var nonCurrentIds = await GetPoolUnassignedNotVersionAsync(resourcePool, dropCount, logger);

                    logger.FluentAddValue("VersionDropFoundCount", nonCurrentIds.Count());

                    // Delete each of the items that are not current
                    foreach (var nonCurrentId in nonCurrentIds)
                    {
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_run_delete",
                            (childLogger) => DeletetPoolItemAsync(Guid.Parse(nonCurrentId), childLogger),
                            logger);
                    }
                }
            }
        }

        private Task<int> GetPoolReadyUnassignedCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolReadyUnassignedCountAsync(
                resourcePool.Details.GetPoolDefinition(), logger.NewChildLogger());
        }

        private Task<int> GetPoolUnassignedNotVersionCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedNotVersionCountAsync(
                resourcePool.Details.GetPoolDefinition(), resourcePool.Details.GetPoolVersionDefinition(), logger.NewChildLogger());
        }

        private Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(ResourcePool resourcePool, int count, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedNotVersionAsync(
                resourcePool.Details.GetPoolDefinition(), resourcePool.Details.GetPoolVersionDefinition(), count, logger.NewChildLogger());
        }

        private async Task DeletetPoolItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            var reason = "TaskVersionChange";

            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, reason);

            await ResourceContinuationOperations.DeleteAsync(null, id, reason, logger.NewChildLogger());
        }
    }
}
