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
            // Create base logger
            var logger = Logger.FromExisting(true);

            // Get Currnet catalog
            var resourceUnits = await RetrieveResourceSkus();
            foreach (var resourceUnit in resourceUnits)
            {
                // Spawn out the tasks and run in parallel
                BackgroundJobs.Create(() => RunTask(resourceUnit, logger), EnqueuedState);
            }
        }

        /// <summary>
        /// This is public due to the need to HangFire to use public methods.
        /// </summary>
        /// <param name="resourcePoolDefinition">Definition of the pool that is being processed.</param>
        /// <param name="logger">Logger that should be used.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task RunTask(ResourcePoolDefinition resourcePoolDefinition, IDiagnosticsLogger logger)
        {
            // Setup logging detials
            logger = logger.WithValues(new LogValueSet
                {
                    { "ActivityInstanceId", Guid.NewGuid().ToString() },
                    { ResourceLoggingConstants.ResourceLocation, resourcePoolDefinition.Location },
                    { ResourceLoggingConstants.ResourceSkuName, resourcePoolDefinition.SkuName },
                    { ResourceLoggingConstants.ResourceType, resourcePoolDefinition.Type.ToString() },
                });
            var duration = logger.StartDuration();

            // Obtain a leas if no one else has it
            using (var lease = await ObtainLease($"{ReadPoolSizeLease}-{resourcePoolDefinition.BuildName()}"))
            {
                // If we couldn't obtain a lease, move on
                if (lease == null)
                {
                    // Send logging detials
                    logger.AddDuration(duration).LogInfo($"watch_pool_size_lease_not_found");

                    return;
                }

                // Determine the effective size of the pool
                var unassignedCount = await RetrieveUnassignedCount(resourcePoolDefinition);

                // Get the desiered pool target size
                var poolTargetCount = resourcePoolDefinition.TargetCount;

                // Get the delta of how many
                var poolDeltaCount = poolTargetCount - unassignedCount;

                // Send logging detials
                logger.WithValues(new LogValueSet
                    {
                        { "checkUnassignedCount", unassignedCount.ToString() },
                        { "checkPoolTargetCount", poolTargetCount.ToString() },
                        { "checkPoolDeltaCount", poolDeltaCount.ToString() },
                    }).AddDuration(duration).LogInfo($"watch_pool_size_lease_obtained");

                // If we have any positive delta add that many jobs to the queue for processing
                if (poolDeltaCount > 0)
                {
                    for (var i = 0; i < poolDeltaCount; i++)
                    {
                        var loggerItem = logger.WithValue("ActivityInstanceIteration", i.ToString());

                        await RunItemTask(resourcePoolDefinition, loggerItem);
                    }
                }
                else if (poolDeltaCount < 0)
                {
                    // TODO: not doing anything with the case where we are over capacity atm
                }
            }
        }

        private async Task RunItemTask(ResourcePoolDefinition resourcePoolDefinition, IDiagnosticsLogger logger)
        {
            // Setup logging detials
            var duration = Logger.StartDuration();

            try
            {
                // Add job definition to the queue
                await ResourceManager.AddResourceCreationRequestToJobQueueAsync(
                    resourcePoolDefinition.SkuName,
                    resourcePoolDefinition.Type,
                    resourcePoolDefinition.Location,
                    logger);

                logger.AddDuration(duration).LogInfo("watch_pool_size_item_complete");
            }
            catch (Exception e)
            {
                logger.AddDuration(duration).LogException("watch_pool_size_item_complete", e);

                // We aren't doing anything with the exception here, we are logging that we
                // didn't complete and we don't want to kill the whole ask because of one
                // fail.
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


    }
}
