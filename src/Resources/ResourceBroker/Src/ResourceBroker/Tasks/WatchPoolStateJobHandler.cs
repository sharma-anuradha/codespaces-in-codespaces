// <copyright file="WatchPoolStateJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that takes regular snapshot of the state of the resource pool.
    /// </summary>
    public class WatchPoolStateJobHandler : WatchPoolJobHandlerBase<WatchPoolStateJobHandler>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolStateJobHandler"/> class.
        /// </summary>
        /// <param name="resourcePoolDefinitionStore">Resource pool definition store.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourcePoolStateSnapshotRepository">Target resource pool state snapshot repository.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="requestQueueProvider">Request Queue Provider.</param>
        public WatchPoolStateJobHandler(
            IResourcePoolDefinitionStore resourcePoolDefinitionStore,
            IResourceRepository resourceRepository,
            IResourcePoolStateSnapshotRepository resourcePoolStateSnapshotRepository,
            ITaskHelper taskHelper,
            IResourceRequestQueueProvider requestQueueProvider)
            : base(resourcePoolDefinitionStore, taskHelper)
        {
            ResourceRepository = resourceRepository;
            ResourcePoolStateSnapshotRepository = resourcePoolStateSnapshotRepository;
            RequestQueueProvider = requestQueueProvider;
        }

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolStateSnapshotRepository ResourcePoolStateSnapshotRepository { get; }

        private IResourceRequestQueueProvider RequestQueueProvider { get; }

        private string LogBaseName => ResourceLoggingConstants.WatchPoolStateJobHandler;

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(ResourcePool resourcePool, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Gets code and version
            var poolCode = resourcePool.Details.GetPoolDefinition();
            var poolVersionCode = resourcePool.Details.GetPoolVersionDefinition();

            // Pulls out the interesting data
            var poolUnassignedCount = await ResourceRepository.GetPoolUnassignedCountAsync(poolCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolUnassignedCount", poolUnassignedCount);

            var poolUnassignedVersionCount = await ResourceRepository.GetPoolUnassignedVersionCountAsync(poolCode, poolVersionCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolUnassignedVersionCount", poolUnassignedVersionCount);

            var poolUnassignedNotVersionCount = await ResourceRepository.GetPoolUnassignedNotVersionCountAsync(poolCode, poolVersionCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolUnassignedNotVersionCount", poolUnassignedNotVersionCount);

            var poolReadyUnassignedCount = await ResourceRepository.GetPoolReadyUnassignedCountAsync(poolCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolReadyUnassignedCount", poolReadyUnassignedCount);

            var poolReadyUnassignedVersionCount = await ResourceRepository.GetPoolReadyUnassignedVersionCountAsync(poolCode, poolVersionCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolReadyUnassignedVersionCount", poolReadyUnassignedVersionCount);

            var poolReadyUnassignedNotVersionCount = await ResourceRepository.GetPoolReadyUnassignedNotVersionCountAsync(poolCode, poolVersionCode, logger.WithValues(new LogValueSet()));
            logger.FluentAddValue("PoolReadyUnassignedNotVersionCount", poolReadyUnassignedNotVersionCount);

            var isAtTargetCount = poolUnassignedVersionCount == resourcePool.TargetCount;
            logger.FluentAddValue("PoolIsAtTargetCount", isAtTargetCount);

            var isReadyAtTargetCount = poolReadyUnassignedVersionCount == resourcePool.TargetCount;
            logger.FluentAddValue("PoolIsReadyAtTargetCount", isReadyAtTargetCount);

            var pendingRequestCount = await RequestQueueProvider.GetPendingRequestCountForPoolAsync(poolCode, logger.NewChildLogger());
            logger.FluentAddValue("PoolPendingRequestCount", pendingRequestCount);

            // Setup model
            var record = new ResourcePoolStateSnapshotRecord()
            {
                Id = poolCode,
                VersionCode = poolVersionCode,
                TargetCount = resourcePool.TargetCount,
                OverrideTargetCount = resourcePool.OverrideTargetCount,
                IsAtTargetCount = isAtTargetCount,
                IsReadyAtTargetCount = isReadyAtTargetCount,
                UnassignedCount = poolUnassignedCount,
                UnassignedVersionCount = poolUnassignedVersionCount,
                UnassignedNotVersionCount = poolUnassignedNotVersionCount,
                ReadyUnassignedCount = poolReadyUnassignedCount,
                ReadyUnassignedVersionCount = poolReadyUnassignedVersionCount,
                ReadyUnassignedNotVersionCount = poolReadyUnassignedNotVersionCount,
                PendingRquestCount = pendingRequestCount,
                Dimensions = resourcePool.Details.GetPoolDimensions(),
                IsEnabled = resourcePool.IsEnabled,
                OverrideIsEnabled = resourcePool.OverrideIsEnabled,
                Updated = DateTime.UtcNow,
            };

            // Save data back
            await ResourcePoolStateSnapshotRepository.CreateOrUpdateAsync(record, logger.WithValues(new LogValueSet()));
        }
    }
}
