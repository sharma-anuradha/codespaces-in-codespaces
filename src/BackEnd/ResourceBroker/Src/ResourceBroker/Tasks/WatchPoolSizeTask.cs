// <copyright file="WatchPoolSizeTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    ///
    /// </summary>
    public class WatchPoolSizeTask : IWatchPoolSizeTask
    {
        private const string ReadPoolSizeLease = nameof(ReadPoolSizeLease);

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchPoolSizeTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings"></param>
        /// <param name="continuationTaskActivator"></param>
        /// <param name="distributedLease"></param>
        /// <param name="resourceScalingStore"></param>
        /// <param name="resourceRepository"></param>
        /// <param name="taskHelper"></param>
        public WatchPoolSizeTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IContinuationTaskActivator continuationTaskActivator,
            IDistributedLease distributedLease,
            IResourceScalingStore resourceScalingStore,
            IResourceRepository resourceRepository,
            ITaskHelper taskHelper)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ContinuationTaskActivator = continuationTaskActivator;
            DistributedLease = distributedLease;
            ResourceScalingStore = resourceScalingStore;
            ResourceRepository = resourceRepository;
            TaskHelper = taskHelper;
        }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private IDistributedLease DistributedLease { get; }

        private IResourceScalingStore ResourceScalingStore { get; }

        private IResourceRepository ResourceRepository { get; }

        private ITaskHelper TaskHelper { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public async Task<bool> RunAsync(IDiagnosticsLogger rootLogger)
        {
            var logger = rootLogger.WithValues(new LogValueSet());

            // Get Currnet catalog
            var resourceUnits = await RetrieveResourceSkus();

            logger.FluentAddValue("CountResourceUnits", resourceUnits.Count().ToString());

            // Run through found resources
            foreach (var resourceUnit in resourceUnits)
            {
                // Spawn out the tasks and run in parallel
                TaskHelper.RunBackground(
                    "watch-pool-size-unit",
                    (childLogger) => RunPoolCheckAsync(resourceUnit, childLogger),
                    rootLogger);
            }

            return !Disposed;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task RunPoolCheckAsync(ResourcePoolDefinition resourcePoolDefinition, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ActivityInstanceId", Guid.NewGuid().ToString())
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceLocation, resourcePoolDefinition.Location)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceSkuName, resourcePoolDefinition.SkuName)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceType, resourcePoolDefinition.Type.ToString());

            // Obtain a leas if no one else has it
            using (var lease = await ObtainLease($"{ReadPoolSizeLease}-{resourcePoolDefinition.BuildName()}"))
            {
                // If we couldn't obtain a lease, move on
                if (lease == null)
                {
                    logger.FluentAddValue("LeaseNotFound", true.ToString());

                    return;
                }

                // Determine the effective size of the pool
                var unassignedCount = await RetrieveUnassignedCount(resourcePoolDefinition, logger.WithValues(new LogValueSet()));

                // Get the desiered pool target size
                var poolTargetCount = resourcePoolDefinition.TargetCount;

                // Get the delta of how many
                var poolDeltaCount = poolTargetCount - unassignedCount;

                logger.FluentAddValue("CheckUnassignedCount", unassignedCount.ToString())
                    .FluentAddValue("CheckPoolTargetCount", poolTargetCount.ToString())
                    .FluentAddValue("CheckPoolDeltaCount", poolDeltaCount.ToString());

                // If we have any positive delta add that many jobs to the queue for processing
                if (poolDeltaCount > 0)
                {
                    for (var i = 0; i < poolDeltaCount; i++)
                    {
                        TaskHelper.RunBackground(
                            "watch-pool-size-unit-create",
                            (childLogger) => AddPoolItemAsync(resourcePoolDefinition, i, childLogger),
                            logger);
                    }
                }
                else if (poolDeltaCount < 0)
                {
                    // TODO: not doing anything with the case where we are over capacity atm
                }
            }
        }

        private async Task AddPoolItemAsync(ResourcePoolDefinition resourcePoolDefinition, int iteration, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ActivityInstanceIterationId", iteration.ToString());

            var input = new CreateResourceContinuationInput()
            {
                Location = resourcePoolDefinition.Location,
                Type = resourcePoolDefinition.Type,
                SkuName = resourcePoolDefinition.SkuName,
            };

            await ContinuationTaskActivator.CreateResource(input, logger);
        }

        private Task<int> RetrieveUnassignedCount(ResourcePoolDefinition resourceSku, IDiagnosticsLogger logger)
        {
            return ResourceRepository.GetUnassignedCountAsync(
                resourceSku.SkuName, resourceSku.Type, resourceSku.Location, logger);
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
            return await DistributedLease.Obtain(ResourceBrokerSettings.LeaseContainerName, leaseName);
        }
    }
}
