// <copyright file="WatchPoolVersionTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
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
    /// Task mananager that tries to tick off a continuation which will try and manage tracking
    /// of the "current" version and conduct orchistrate drains as requried.
    /// </summary>
    public class WatchPoolVersionTask : BaseWatchPoolTask, IWatchPoolVersionTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolVersionTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target reesource broker settings.</param>
        /// <param name="continuationTaskActivator">Target continuation activator.</param>
        /// <param name="distributedLease">Target distributed lease.</param>
        /// <param name="resourceScalingStore">Target resource scaling store.</param>
        /// <param name="resourceRepository">Target resource Repository.</param>
        /// <param name="taskHelper">Target task helper.</param>
        public WatchPoolVersionTask(
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
        protected override string LeaseBaseName => "WatchPoolVersionTaskLease";

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolVersionTask;

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected async override Task RunPoolActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            // Check database for non current count
            var unassignedNotVersionCount = await GetPoolUnassignedNotVersionCountAsync(resourcePool, logger);
            if (unassignedNotVersionCount > 0)
            {
                // See how many are current ready for use in the pool
                var readyUnassignedCount = await GetPoolReadyUnassignedCountAsync(resourcePool, logger);

                // We only will do anything if the pool has 80% resources ready for actual use
                if ((double)readyUnassignedCount / resourcePool.TargetCount >= 0.8)
                {
                    // Get 20% items to delete
                    var dropCount = (int)Math.Ceiling(resourcePool.TargetCount * 0.2);
                    var nonCurrentIds = await GetPoolUnassignedNotVersionAsync(resourcePool, dropCount, logger);

                    // Delete each of the items that are not current
                    foreach (var nonCurrentId in nonCurrentIds)
                    {
                        TaskHelper.RunBackground(
                            $"{LogBaseName}_delete",
                            (childLogger) => DeletetPoolItemAsync(Guid.Parse(nonCurrentId), childLogger),
                            logger);
                    }
                }
            }
        }

        private Task<int> GetPoolReadyUnassignedCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolReadyUnassignedCountAsync(
                resourcePool.Details.GetPoolDefinition(), logger.WithValues(new LogValueSet()));
        }

        private Task<int> GetPoolUnassignedNotVersionCountAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedNotVersionCountAsync(
                resourcePool.Details.GetPoolDefinition(), resourcePool.Details.GetPoolVersionDefinition(), logger.WithValues(new LogValueSet()));
        }

        private Task<IEnumerable<string>> GetPoolUnassignedNotVersionAsync(ResourcePool resourcePool, int count, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetPoolUnassignedNotVersionAsync(
                resourcePool.Details.GetPoolDefinition(), resourcePool.Details.GetPoolVersionDefinition(), count, logger.WithValues(new LogValueSet()));
        }

        private async Task DeletetPoolItemAsync(Guid id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ActivityInstanceIterationId", id);

            await ContinuationTaskActivator.DeleteResource(id, "TaskVersionChange", logger);
        }
    }
}
