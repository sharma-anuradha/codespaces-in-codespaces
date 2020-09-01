// <copyright file="WatchPoolStateTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Task mananager that takes regular snapshot of the state of the resource pool.
    /// </summary>
    public class WatchPoolStateTask : BaseWatchPoolTask, IWatchPoolStateTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolStateTask"/> class.
        /// </summary>
        /// <param name="resourcePoolManager">Target resource pool manager.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourcePoolStateSnapshotRepository">Target resource pool state snapshot repository.</param>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceScalingStore">Resource scalling store.</param>
        /// <param name="claimedDistributedLease">Distributed lease.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="resourceNameBuilder">Target resource name builder.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        /// <param name="requestQueueProvider">Request Queue Provider.</param>
        public WatchPoolStateTask(
            IResourcePoolManager resourcePoolManager,
            IResourceRepository resourceRepository,
            IResourcePoolStateSnapshotRepository resourcePoolStateSnapshotRepository,
            ResourceBrokerSettings resourceBrokerSettings,
            IResourcePoolDefinitionStore resourceScalingStore,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader,
            IResourceRequestQueueProvider requestQueueProvider)
            : base(resourceBrokerSettings, resourceScalingStore, claimedDistributedLease, taskHelper, resourceNameBuilder, jobSchedulerFeatureFlags, configurationReader)
        {
            ResourcePoolManager = resourcePoolManager;
            ResourceRepository = resourceRepository;
            ResourcePoolStateSnapshotRepository = resourcePoolStateSnapshotRepository;
            RequestQueueProvider = requestQueueProvider;
        }

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchPoolStateTask";

        /// <inheritdoc/>
        protected override string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchPoolStateTask)}Lease");

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.WatchPoolStateTask;

        private IResourcePoolManager ResourcePoolManager { get; }

        private IResourceRepository ResourceRepository { get; }

        private IResourcePoolStateSnapshotRepository ResourcePoolStateSnapshotRepository { get; }

        private IResourceRequestQueueProvider RequestQueueProvider { get; }

        /// <inheritdoc/>
        protected override async Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
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
