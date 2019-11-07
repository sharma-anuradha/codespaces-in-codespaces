// <copyright file="BaseDataPlaneResourceGroupTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for background tasks that operate on all data plane resource groups for a control plane stamp.
    /// </summary>
    public abstract class BaseDataPlaneResourceGroupTask : IBackgroundTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDataPlaneResourceGroupTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="capacityManager">Target capacity manager.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public BaseDataPlaneResourceGroupTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            ICapacityManager capacityManager,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            TaskHelper = taskHelper;
            CapacityManager = capacityManager;
            ClaimedDistributedLease = claimedDistributedLease;
            ResourceNameBuilder = resourceNameBuilder;
        }

        /// <summary>
        /// Gets the name of the task. Usually the name of the implementing class.
        /// </summary>
        protected abstract string TaskName { get; }

        /// <summary>
        /// Gets the base message name to use for log messages in this task.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Loop delay.
        /// </summary>
        protected static TimeSpan LoopDelay = TimeSpan.FromMilliseconds(500);

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{TaskName}Lease");

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private ITaskHelper TaskHelper { get; }

        private ICapacityManager CapacityManager { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool Disposed { get; set; }


        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    var dataPlaneResourceGroups = await RetrieveResourceGroups();

                    logger.FluentAddValue("TaskCountResourceGroups", dataPlaneResourceGroups.Count().ToString());

                    // Run through found resources in the background
                    await TaskHelper.RunEnumerableAsync(
                        $"{LogBaseName}_run_resourcegroup",
                        dataPlaneResourceGroups,
                        (resourceGroup, itemLogger) => CoreRunResourceGroupAsync(resourceGroup, itemLogger),
                        childLogger,
                        (resourceGroup, itemLogger) => ObtainLease($"{LeaseBaseName}-{resourceGroup.Subscription.SubscriptionId}-{resourceGroup.ResourceGroup}", taskInterval, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => !Disposed,
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Process an individual resource group.
        /// </summary>
        /// <param name="resourceGroup">The resource group to process.</param>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        protected abstract Task ProcessResourceGroupAsync(IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger);

        private async Task CoreRunResourceGroupAsync(IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskResourceSubscription", resourceGroup.Subscription.SubscriptionId)
                .FluentAddBaseValue("TaskResourceGroup", resourceGroup.ResourceGroup);

            // Executes the action that needs to be performed on the pool
            await logger.TrackDurationAsync(
                "RunResourceGroupAction", () => ProcessResourceGroupAsync(resourceGroup, logger));
        }

        private async Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups()
        {
            var resourceGroups = await CapacityManager.GetAllDataPlaneResourceGroups();
            return resourceGroups.Shuffle();
        }

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}
