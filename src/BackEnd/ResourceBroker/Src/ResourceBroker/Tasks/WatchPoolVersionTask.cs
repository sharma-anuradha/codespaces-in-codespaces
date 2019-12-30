// <copyright file="WatchPoolVersionTask.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that tries to tick off a continuation which will try and manage tracking
    /// of the "current" version and conduct orchistrate drains as requried.
    /// </summary>
    public class WatchPoolVersionTask : BaseWatchPoolTask, IWatchPoolVersionTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolVersionTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target reesource broker settings.</param>
        /// <param name="resourceContinuationOperations">Target continuation activator.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource Repository.</param>
        /// <param name="claimedDistributedLease">Target distributed lease.</param>
        /// <param name="taskHelper">Target task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchPoolVersionTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            IResourcePoolDefinitionStore resourceScalingStore,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder)
            : base(resourceBrokerSettings, resourceScalingStore, claimedDistributedLease, taskHelper, resourceNameBuilder)
        {
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchPoolVersionTask)}Lease");

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolVersionTask;

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected async override Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
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

            await ResourceContinuationOperations.DeleteResource(id, reason, logger.NewChildLogger());
        }
    }
}
