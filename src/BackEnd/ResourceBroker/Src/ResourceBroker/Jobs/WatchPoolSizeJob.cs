// <copyright file="WatchPoolSizeJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.States;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using static Microsoft.VsSaaS.Diagnostics.Extensions.DiagnosticsLoggerExtensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Jobs
{
    /// <summary>
    /// 
    /// </summary>
    public class WatchPoolSizeJob
    {
        public const string QueueName = "pool-size-resource-job-queue";

        private const string ReadPoolSizeLease = nameof(ReadPoolSizeLease);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolSizeJob"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings"></param>
        /// <param name="backgroundJobs"></param>
        /// <param name="distributedLease"></param>
        /// <param name="resourceScalingStore"></param>
        /// <param name="resourceManager"></param>
        /// <param name="resourceRepository"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="logValues"></param>
        public WatchPoolSizeJob(
            ResourceBrokerSettings resourceBrokerSettings,
            IBackgroundJobClient backgroundJobs,
            IDistributedLease distributedLease,
            IResourceScalingStore resourceScalingStore,
            IResourceManager resourceManager,
            IResourceRepository resourceRepository,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet logValues)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            BackgroundJobs = backgroundJobs;
            DistributedLease = distributedLease;
            ResourceScalingStore = resourceScalingStore;
            ResourceManager = resourceManager;
            ResourceRepository = resourceRepository;
            Logger = loggerFactory.New(logValues);
            EnqueuedState = new EnqueuedState
            {
                Queue = QueueName,
            };
        }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IBackgroundJobClient BackgroundJobs { get; }

        private IDistributedLease DistributedLease { get; }

        private IResourceScalingStore ResourceScalingStore { get; }

        private IResourceManager ResourceManager { get; }

        private IResourceRepository ResourceRepository { get; }

        private IDiagnosticsLogger Logger { get; }

        private IState EnqueuedState { get; }

        /// <summary>
        /// This job, for each resource sku we have, will move through those resoruces randomly
        /// and in parallel. As each resource is processed, it will attempt to obtain a lock on
        /// that resource, if it can't obtain a lock, it will continue onto the next item (as its
        /// assumed another worker is successfully working on that data), if it can obtain a lock,
        /// it will determine how many items need to be added to the job queue and add those items
        /// to the queue.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task Run()
        {
            // Get Currnet catalog
            var resourceUnits = await RetrieveResourceSkus();
            foreach (var resourceUnit in resourceUnits)
            {
                // Spawn out the tasks and run in parallel
                BackgroundJobs.Create(() => RunTask(resourceUnit), EnqueuedState);
            }
        }

        /// <summary>
        /// This is public due to the need to HangFire to use public methods.
        /// </summary>
        /// <param name="resourcePoolDefinition">Definition of the pool that is being processed.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task RunTask(ResourcePoolDefinition resourcePoolDefinition)
        {
            // Setup logging detials
            var taskId = Guid.NewGuid();
            var duration = Logger
                .StartDuration();

            // Obtain a leas if no one else has it
            using (var lease = await ObtainLease($"{ReadPoolSizeLease}-{resourcePoolDefinition.BuildName()}"))
            {
                // If we couldn't obtain a lease, move on
                if (lease == null)
                {
                    // Send logging detials
                    LogDetails("lease_nofound", taskId, duration, resourcePoolDefinition);

                    return;
                }

                // Determine the effective size of the pool
                var unassignedCount = await RetrieveUnassignedCount(resourcePoolDefinition);

                // Get the desiered pool target size
                var poolTargetCount = resourcePoolDefinition.TargetCount;

                // Get the delta of how many
                var poolDeltaCount = poolTargetCount - unassignedCount;

                // Send logging detials
                var loggingProperties = new Dictionary<string, object>()
                    {
                        { "checkUnassignedCount", unassignedCount },
                        { "checkPoolTargetCount", poolTargetCount },
                        { "checkPoolDeltaCount", poolDeltaCount },
                    };
                LogDetails("lease_obtained", taskId, duration, resourcePoolDefinition, loggingProperties);

                // If we have any positive delta add that many jobs to the queue for processing
                if (poolDeltaCount > 0)
                {
                    for (var i = 0; i < poolDeltaCount; i++)
                    {
                        await RunItemTask(resourcePoolDefinition, taskId, i);
                    }
                }
                else if (poolDeltaCount < 0)
                {
                    // TODO: not doing anything with the case where we are over capacity atm
                }
            }
        }

        private async Task RunItemTask(ResourcePoolDefinition resourcePoolDefinition, Guid taskId, int index)
        {
            // Setup logging detials
            var duration = Logger
                .StartDuration();

            try
            {
                // Add job definition to the queue
                await ResourceManager.AddResourceCreationRequestToJobQueueAsync(
                    resourcePoolDefinition.SkuName,
                    resourcePoolDefinition.Type,
                    resourcePoolDefinition.Location,
                    Logger);

                LogItemDetails("complete", taskId, index, duration);
            }
            catch (Exception e)
            {
                LogItemDetails("error", taskId, index, duration, e);
            }

        }

        private Task<int> RetrieveUnassignedCount(ResourcePoolDefinition resourceSku)
        {
            return ResourceRepository.GetUnassignedCountAsync(resourceSku.SkuName, resourceSku.Type, resourceSku.Location, Logger);
        }

        private async Task<IEnumerable<ResourcePoolDefinition>> RetrieveResourceSkus()
        {
            var resourceUnits = (await ResourceScalingStore.RetrieveLatestScaleLevels())
                .ToList()
                .Shuffle();

            return resourceUnits;
        }

        private async Task<IDisposable> ObtainLease(string leaseName)
        {
            return await DistributedLease.Obtain(ResourceBrokerSettings.BlobContainerName, leaseName);
        }

        private void LogDetails(
            string logName,
            Guid taskId,
            Duration duration,
            ResourcePoolDefinition resourcePoolDefinition,
            IDictionary<string, object> properties = null)
        {
            Logger
                .AddDuration(duration)
                .AddTaskId(taskId)
                .AddResourceLocation(resourcePoolDefinition.Location)
                .AddResourceSku(resourcePoolDefinition.SkuName)
                .AddResourceType(resourcePoolDefinition.Type)
                .AddProperties(properties)
                .LogInfo($"watch_pool_size_{logName}");
        }

        private void LogItemDetails(
            string logName,
            Guid taskId,
            int iterationId,
            Duration duration,
            Exception exception = null)
        {
            Logger
                .AddDuration(duration)
                .AddTaskId(taskId)
                .AddIterationId(iterationId);

            if (exception != null)
            {
                Logger.LogException($"watch_pool_size_item_{logName}", exception);
            }
            else
            {
                Logger.LogInfo($"watch_pool_size_item_{logName}");
            }
        }
    }
}
