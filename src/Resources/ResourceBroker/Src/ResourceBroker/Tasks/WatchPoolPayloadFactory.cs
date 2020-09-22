// <copyright file="WatchPoolPayloadFactory.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// A class that implements IJobSchedulePayloadFactory and able to produce job payloads
    /// for the watch pool consumer handlers.
    /// </summary>
    public class WatchPoolPayloadFactory : IJobSchedulePayloadFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolPayloadFactory"/> class.
        /// </summary>
        /// <param name="resourceScalingStore">Resource scalling store.</param>
        /// <param name="taskHelper">Task helper.</param>
        public WatchPoolPayloadFactory(
            IResourcePoolDefinitionStore resourceScalingStore,
            ITaskHelper taskHelper)
        {
            ResourceScalingStore = resourceScalingStore;
            TaskHelper = taskHelper;
        }

        /// <summary>
        /// Gets the task ehlper.
        /// </summary>
        private ITaskHelper TaskHelper { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        /// <inheritdoc/>
        public async Task CreatePayloadsAsync(string jobRunId, DateTime scheduleRun, IServiceProvider serviceProvider, OnPayloadCreatedDelegate onPayloadCreated, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Get current catalog
            var resourceUnits = await RetrieveResourceSkus();
            var jobInvisibleDeltaInMillisecs = (60 * 1000) / (4 * resourceUnits.Count());
            var payloadVisibilityDelayHelper = new PayloadVisibilityDelayHelper(TimeSpan.FromMilliseconds(jobInvisibleDeltaInMillisecs));

            logger.FluentAddValue("TaskCountResourceUnits", resourceUnits.Count().ToString());

            // Run through found resources in the background
            await TaskHelper.RunEnumerableAsync(
                $"{ResourceLoggingConstants.WatchPoolProducerTask}_run_unit_check",
                resourceUnits,
                async (resourceUnit, itemLogger) =>
                {
                    await CreateResourcePoolJobsAsync(resourceUnit, () => payloadVisibilityDelayHelper.NextValue(), onPayloadCreated, itemLogger);
                },
                logger);
        }

        private Task CreateResourcePoolJobsAsync(
            ResourcePool resourcePool,
            Func<TimeSpan> payloadVisibilitCallback,
            OnPayloadCreatedDelegate onPayloadCreated,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolLocation, resourcePool.Details.Location.ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolSkuName, resourcePool.Details.SkuName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolResourceType, resourcePool.Type.ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolDefinition, resourcePool.Details.GetPoolDefinition())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolVersionDefinition, resourcePool.Details.GetPoolVersionDefinition())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolTargetCount, resourcePool.TargetCount)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolOverrideTargetCount, resourcePool.OverrideTargetCount)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolIsEnabled, resourcePool.IsEnabled)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolOverrideIsEnabled, resourcePool.OverrideIsEnabled)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageFamilyName, resourcePool.Details.ImageFamilyName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.PoolImageName, resourcePool.Details.ImageName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.MaxCreateBatchCount, resourcePool.MaxCreateBatchCount)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.MaxDeleteBatchCount, resourcePool.MaxDeleteBatchCount);

            return Task.WhenAll(
                CreateResourcePoolJobAsync<WatchPoolSizeJobHandler>(resourcePool, payloadVisibilitCallback, onPayloadCreated),
                CreateResourcePoolJobAsync<WatchPoolStateJobHandler>(resourcePool, payloadVisibilitCallback, onPayloadCreated),
                CreateResourcePoolJobAsync<WatchPoolVersionJobHandler>(resourcePool, payloadVisibilitCallback, onPayloadCreated),
                CreateResourcePoolJobAsync<WatchFailedResourcesJobHandler>(resourcePool, payloadVisibilitCallback, onPayloadCreated));
        }

        private Task CreateResourcePoolJobAsync<TJobHandlerType>(
            ResourcePool resourcePool,
            Func<TimeSpan> payloadVisibilitCallback,
            OnPayloadCreatedDelegate onPayloadCreated)
            where TJobHandlerType : class
        {
            var jobPayload = new ResourcePoolPayload<TJobHandlerType>() { PoolId = resourcePool.Id };
            var jobPayloadOptions = new JobPayloadOptions()
            {
                InitialVisibilityDelay = payloadVisibilitCallback(),
                ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
            };

            return onPayloadCreated(jobPayload, jobPayloadOptions);
        }

        private async Task<IEnumerable<ResourcePool>> RetrieveResourceSkus()
        {
            return (await ResourceScalingStore.RetrieveDefinitionsAsync()).Shuffle();
        }

        /// <summary>
        /// A resource pool payload.
        /// </summary>
        /// <typeparam name="T">Type of the job handler.</typeparam>
        public class ResourcePoolPayload<T> : JobPayload<T>
            where T : class
        {
            /// <summary>
            /// Gets or sets the pool id.
            /// </summary>
            public string PoolId { get; set; }
        }

        private class PayloadVisibilityDelayHelper
        {
            private readonly TimeSpan space;
            private TimeSpan next = TimeSpan.Zero;

            public PayloadVisibilityDelayHelper(TimeSpan space)
            {
                this.space = space;
            }

            public TimeSpan NextValue()
            {
                TimeSpan result = this.next;
                this.next = this.next.Add(this.space);
                return result;
            }
        }
    }
}
