// <copyright file="BaseWatchPoolTask.cs" company="Microsoft">
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
        /// <param name="claimedDistributedLease">Cleaimed distributed lease.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public BaseWatchPoolTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourcePoolDefinitionStore resourceScalingStore,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceScalingStore = resourceScalingStore;
            ClaimedDistributedLease = claimedDistributedLease;
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

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async () =>
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
                            (childLogger) => RunPoolAsync(resourceUnit, claimSpan, childLogger),
                            logger);
                    }

                    return !Disposed;
                },
                (e) => !Disposed,
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Action that needs to be performed when a lock is granted on the target pool.
        /// </summary>
        /// <param name="resourcePool">Current pool definition.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Running task.</returns>
        protected abstract Task RunActionAsync(ResourcePool resourcePool, IDiagnosticsLogger logger);

        private Task RunPoolAsync(ResourcePool resourcePool, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                .FluentAddBaseValue("PoolLocation", resourcePool.Details.Location.ToString())
                .FluentAddBaseValue("PoolSkuName", resourcePool.Details.SkuName)
                .FluentAddBaseValue("PoolResourceType", resourcePool.Type.ToString())
                .FluentAddBaseValue("PoolDefinition", resourcePool.Details.GetPoolDefinition())
                .FluentAddBaseValue("PoolVersionDefinition", resourcePool.Details.GetPoolVersionDefinition())
                .FluentAddBaseValue("PoolTargetCount", resourcePool.TargetCount)
                .FluentAddBaseValue("PoolImageFamilyName", resourcePool.Details.ImageFamilyName)
                .FluentAddBaseValue("PoolImageName", resourcePool.Details.ImageName);

            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_unit_check",
                async () =>
                {
                    // Obtain a lease if no one else has it
                    using (var lease = await ObtainLease($"{LeaseBaseName}-{resourcePool.Details.GetPoolDefinition()}", claimSpan, logger))
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
                },
                (e) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        private async Task<IEnumerable<ResourcePool>> RetrieveResourceSkus()
        {
            var resourceUnits = (await ResourceScalingStore.RetrieveDefinitions()).Shuffle();

            return resourceUnits;
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName,
                leaseName,
                claimSpan,
                logger.WithValues(new LogValueSet()));
        }
    }
}
