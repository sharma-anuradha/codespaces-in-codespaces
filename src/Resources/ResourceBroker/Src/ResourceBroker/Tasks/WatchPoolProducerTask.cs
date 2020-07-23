// <copyright file="WatchPoolProducerTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Defines a task which is designed to watch the pools for versious changes.
    /// </summary>
    public class WatchPoolProducerTask : IWatchPoolProducerTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolProducerTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceScalingStore">Resource scalling store.</param>
        /// <param name="claimedDistributedLease">Cleaimed distributed lease.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="jobQueueProducerFactory">The job queue producer factory.</param>
        public WatchPoolProducerTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourcePoolDefinitionStore resourceScalingStore,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder,
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceScalingStore = resourceScalingStore;
            ClaimedDistributedLease = claimedDistributedLease;
            TaskHelper = taskHelper;
            ResourceNameBuilder = resourceNameBuilder;
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(ResourceJobQueueConstants.GenericQueueName);
        }

        /// <summary>
        /// Gets the target resource broker settings.
        /// </summary>
        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        /// <summary>
        /// Gets the task ehlper.
        /// </summary>
        private ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets the resource name builder.
        /// </summary>
        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IJobQueueProducer JobQueueProducer { get; }

        private bool Disposed { get; set; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchPoolProducerTask)}");

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{ResourceLoggingConstants.WatchPoolProducerTask}_run",
                async (childLogger) =>
                {
                    // Obtain lease so trigger task is only added once
                    using (var lease = await ObtainLeaseAsync($"{LeaseBaseName}-lease", claimSpan, childLogger))
                    {
                        if (lease != null)
                        {
                            // Get current catalog
                            var resourceUnits = await RetrieveResourceSkus();
                            var jobInvisibleDeltaInMillisecs = (60 * 1000) / (4 * resourceUnits.Count());
                            var payloadVisibilityDelayHelper = new PayloadVisibilityDelayHelper(TimeSpan.FromMilliseconds(jobInvisibleDeltaInMillisecs));

                            childLogger.FluentAddValue("TaskCountResourceUnits", resourceUnits.Count().ToString());

                            // Run through found resources in the background
                            await TaskHelper.RunEnumerableAsync(
                                $"{ResourceLoggingConstants.WatchPoolProducerTask}_run_unit_check",
                                resourceUnits,
                                (resourceUnit, itemLogger) => PublishResourcePoolJobsAsync(resourceUnit, payloadVisibilityDelayHelper, itemLogger),
                                childLogger);
                        }

                        return !Disposed;
                    }
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task PublishResourcePoolJobsAsync(ResourcePool resourcePool, PayloadVisibilityDelayHelper payloadVisibilityDelayHelper, IDiagnosticsLogger logger)
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

            // Create multiple jobs for the different pool job handlers processing.
            await PublishResourcePoolPayload<WatchPoolSizeJobHandler>(resourcePool, payloadVisibilityDelayHelper);
            await PublishResourcePoolPayload<WatchPoolStateJobHandler>(resourcePool, payloadVisibilityDelayHelper);
            await PublishResourcePoolPayload<WatchPoolVersionJobHandler>(resourcePool, payloadVisibilityDelayHelper);
            await PublishResourcePoolPayload<WatchFailedResourcesJobHandler>(resourcePool, payloadVisibilityDelayHelper);
        }

        private Task PublishResourcePoolPayload<TJobHandlerType>(ResourcePool resourcePool, PayloadVisibilityDelayHelper payloadVisibilityDelayHelper)
            where TJobHandlerType : class
        {
            var jobPayload = new ResourcePoolPayload<TJobHandlerType>() { PoolId = resourcePool.Id };
            var jobPayloadOptions = new JobPayloadOptions()
            {
                InitialVisibilityDelay = payloadVisibilityDelayHelper.NextValue(),
                ExpireTimeout = TimeSpan.FromMinutes(2),
            };

            return JobQueueProducer.AddJobAsync(
                jobPayload,
                jobPayloadOptions,
                default);
        }

        private async Task<IEnumerable<ResourcePool>> RetrieveResourceSkus()
        {
            return (await ResourceScalingStore.RetrieveDefinitionsAsync()).Shuffle();
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
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
