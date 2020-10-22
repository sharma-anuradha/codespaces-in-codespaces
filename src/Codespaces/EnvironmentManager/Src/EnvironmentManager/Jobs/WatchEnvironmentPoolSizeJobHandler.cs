// <copyright file="WatchEnvironmentPoolSizeJobHandler.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Jobs
{
    /// <summary>
    /// Task mananager that watches the pool size and determins if any delta operations need to be
    /// performed to fill/drain the pool.
    /// </summary>
    public class WatchEnvironmentPoolSizeJobHandler : WatchEnvironmentPoolJobHandlerBase<WatchEnvironmentPoolSizeJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchEnvironmentPoolSizeJobHandler"/> class.
        /// </summary>
        /// <param name="poolDefinitionStore">Resource pool definition store.</param>
        /// <param name="environmentRepository">Target resource repository.</param>
        /// <param name="continuationOperations">Target continuation activator.</param>
        /// <param name="configReader">Target configuration reader.</param>
        public WatchEnvironmentPoolSizeJobHandler(
            IEnvironmentPoolDefinitionStore poolDefinitionStore,
            ICloudEnvironmentRepository environmentRepository,
            IEnvironmentContinuationOperations continuationOperations,
            IConfigurationReader configReader)
            : base(poolDefinitionStore, configReader)
        {
            EnvironmentRepository = Requires.NotNull(environmentRepository, nameof(environmentRepository));
            EnvironmentContinuationOperations = Requires.NotNull(continuationOperations, nameof(continuationOperations));
        }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        private IEnvironmentContinuationOperations EnvironmentContinuationOperations { get; }

        protected override string LogBaseName => EnvironmentLoggingConstants.WatchEnvironmentPoolSizeTask;

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(EnvironmentPool pool, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            logger.FluentAddValue("PoolIsEnabled", pool.IsEnabled)
                  .FluentAddValue("PoolTargetCount", pool.TargetCount);

            if (!pool.IsEnabled)
            {
                // Watch pool size for active pools only. 
                return;
            }

            // Determine the effective size of the pool
            var unassignedCount = await GetPoolUnassignedCountAsync(pool, logger.NewChildLogger());

            // Get the delta of how many
            var poolDeltaCount = pool.TargetCount - unassignedCount;

            logger.FluentAddValue("SizeCheckUnassignedCount", unassignedCount.ToString())
                .FluentAddValue("SizeCheckPoolDeltaCount", poolDeltaCount.ToString());

            // If we have any positive delta add that many jobs to the queue for processing
            if (poolDeltaCount > 0)
            {
                // Limits the amount that can be created at any one time
                poolDeltaCount = Math.Min(poolDeltaCount, pool.MaxCreateBatchCount);

                // Add each of the times that we need to have
                for (var i = 0; i < poolDeltaCount; i++)
                {
                    await AddPoolItemAsync(pool, i, logger);
                }
            }
            else if (poolDeltaCount < 0)
            {
                // Limits the amount that can be created at any one time
                poolDeltaCount = Math.Min(poolDeltaCount * -1, pool.MaxDeleteBatchCount);

                // Get some items we can delete
                var unassignedIds = await GetPoolUnassignedAsync(pool, poolDeltaCount, logger);

                logger.FluentAddValue("PoolDrainCountFound", unassignedIds.Count());

                // Delete each of the items that are not current
                foreach (var unassignedId in unassignedIds)
                {
                    await DeletePoolItemAsync(Guid.Parse(unassignedId), "WatchPoolSizeDecrease", logger);
                }
            }
        }

        private Task<int> GetPoolUnassignedCountAsync(EnvironmentPool pool, IDiagnosticsLogger logger)
        {
            return EnvironmentRepository.GetPoolUnassignedCountAsync(
                pool.Details.GetPoolDefinition(), logger.NewChildLogger());
        }

        private Task<IEnumerable<string>> GetPoolUnassignedAsync(EnvironmentPool pool, int count, IDiagnosticsLogger logger)
        {
            return EnvironmentRepository.GetPoolUnassignedAsync(
                pool.Details.GetPoolDefinition(), count, logger.NewChildLogger());
        }

        private Task AddPoolItemAsync(EnvironmentPool pool, int iteration, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_add_resource",
                async (innerLogger) =>
            {
                var id = Guid.NewGuid();
                var reason = "WatchPoolSizeIncrease";

                innerLogger.FluentAddBaseValue("TaskJobIteration", iteration.ToString())
                    .FluentAddBaseValue(EnvironmentLoggingPropertyConstants.EnvironmentId, id)
                    .FluentAddBaseValue(EnvironmentLoggingPropertyConstants.OperationReason, reason);

                await EnvironmentContinuationOperations.CreatePoolResourceAsync(id, pool, reason, innerLogger.NewChildLogger());
            });
        }

        private Task DeletePoolItemAsync(Guid id, string reason, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_delete_resource",
                async (innerLogger) =>
                {
                    innerLogger.FluentAddBaseValue(EnvironmentLoggingPropertyConstants.EnvironmentId, id)
                          .FluentAddBaseValue(EnvironmentLoggingPropertyConstants.OperationReason, reason);

                    await EnvironmentContinuationOperations.DeletePoolResourceAsync(id, reason, innerLogger.NewChildLogger());
                });
        }
    }
}
