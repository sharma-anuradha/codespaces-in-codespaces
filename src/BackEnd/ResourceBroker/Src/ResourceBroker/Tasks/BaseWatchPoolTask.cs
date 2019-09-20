﻿// <copyright file="BaseWatchPoolTask.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Defines a task which is designed to watch the pools for versious changes.
    /// </summary>
    public abstract class BaseWatchPoolTask : IWatchPoolTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWatchPoolTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceScalingStore">Resource scalling store.</param>
        /// <param name="distributedLease">Distributed lease.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public BaseWatchPoolTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourcePoolDefinitionStore resourceScalingStore,
            IDistributedLease distributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceScalingStore = resourceScalingStore;
            DistributedLease = distributedLease;
            TaskHelper = taskHelper;
            ResourceNameBuilder = resourceNameBuilder;
        }

        /// <summary>
        /// Gets the target resource broker settings.
        /// </summary>
        protected ResourceBrokerSettings ResourceBrokerSettings { get; }

        /// <summary>
        /// Gets the task ehlper.
        /// </summary>
        protected ITaskHelper TaskHelper { get; }

        /// <summary>
        /// Gets the resource name builder.
        /// </summary>
        protected IResourceNameBuilder ResourceNameBuilder { get; }

        /// <summary>
        /// Gets the base lease name.
        /// </summary>
        protected abstract string LeaseBaseName { get; }

        /// <summary>
        /// Gets the base logging name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        private IResourcePoolDefinitionStore ResourceScalingStore { get; }

        private IDistributedLease DistributedLease { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync($"{LogBaseName}_run", () => CoreRunAsync(logger), swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Action that needs to be performed when a lock is able to be achived on the target pool.
        /// </summary>
        /// <param name="resourcePool">Current pool definition.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Running task.</returns>
        protected abstract Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger);

        private async Task<bool> CoreRunAsync(IDiagnosticsLogger logger)
        {
            // Get current catalog
            var resourceUnits = await RetrieveResourceSkus();

            logger.FluentAddValue("TaskCountResourceUnits", resourceUnits.Count().ToString());

            // Run through found resources
            foreach (var resourceUnit in resourceUnits)
            {
                // Spawn out the tasks and run in parallel
                TaskHelper.RunBackground(
                    $"{LogBaseName}_run_unit_check",
                    (childLogger) =>
                    {
                        return childLogger.OperationScopeAsync(
                            $"{LogBaseName}_run_unit_check",
                            () => CoreRunUnitAsync(resourceUnit, childLogger),
                            swallowException: true);
                    },
                    logger);
            }

            return !Disposed;
        }

        private async Task CoreRunUnitAsync(ResourcePool resourcePool, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                .FluentAddBaseValue("PoolLocation", resourcePool.Details.Location.ToString())
                .FluentAddBaseValue("PoolSkuName", resourcePool.Details.SkuName)
                .FluentAddBaseValue("PoolResourceType", resourcePool.Type.ToString())
                .FluentAddBaseValue("PoolDefinition", resourcePool.Details.GetPoolDefinition())
                .FluentAddBaseValue("PoolVersioDefinition", resourcePool.Details.GetPoolVersionDefinition())
                .FluentAddBaseValue("PoolTargetCount", resourcePool.TargetCount)
                .FluentAddBaseValue("PoolImageFamilyName", resourcePool.Details.ImageFamilyName)
                .FluentAddBaseValue("PoolImageName", resourcePool.Details.ImageName);

            // Obtain a lease if no one else has it
            using (var lease = await ObtainLease($"{LeaseBaseName}-{resourcePool.Details.GetPoolDefinition()}"))
            {
                logger.FluentAddValue("LeaseNotFound", lease == null);

                // If we couldn't obtain a lease, move on
                if (lease == null)
                {
                    return;
                }

                // Executes the action that needs to be performed on the pool
                await logger.TrackDurationAsync(
                    "RunPoolAction", () => RunActionAsync(resourcePool, logger));
            }
        }

        private async Task<IEnumerable<ResourcePool>> RetrieveResourceSkus()
        {
            var resourceUnits = (await ResourceScalingStore.RetrieveDefinitions())
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
