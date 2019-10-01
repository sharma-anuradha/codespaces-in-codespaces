// <copyright file="WatchOrphanedAzureResourceTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned Azure Resource.
    /// </summary>
    public class WatchOrphanedAzureResourceTask : IWatchOrphanedAzureResourceTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedAzureResourceTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceManagementRepository">Target resource management repository.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourceScalingStore">Resource scaling store.</param>
        /// <param name="continuationTaskActivator">Target continuation task activator.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="capacityManager">Target capacity manager.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchOrphanedAzureResourceTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IContinuationTaskActivator continuationTaskActivator,
            ITaskHelper taskHelper,
            ICapacityManager capacityManager,
            IClaimedDistributedLease claimedDistributedLease,
            IAzureClientFactory azureClientFactory,
            IResourceNameBuilder resourceNameBuilder)
        {
            ResourceBrokerSettings = resourceBrokerSettings;
            ResourceRepository = resourceRepository;
            ContinuationTaskActivator = continuationTaskActivator;
            TaskHelper = taskHelper;
            CapacityManager = capacityManager;
            ClaimedDistributedLease = claimedDistributedLease;
            AzureClientFactory = azureClientFactory;
            ResourceNameBuilder = resourceNameBuilder;
        }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{nameof(WatchOrphanedAzureResourceTask)}Lease");

        private string LogBaseName => ResourceLoggingConstants.WatchOrphanedAzureResourceTask;

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IResourceRepository ResourceRepository { get; }

        private IContinuationTaskActivator ContinuationTaskActivator { get; }

        private ITaskHelper TaskHelper { get; }

        private ICapacityManager CapacityManager { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IAzureClientFactory AzureClientFactory { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async () =>
                {
                    await CoreRunAsync(claimSpan, logger);
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

        private async Task<bool> CoreRunAsync(TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            var capacityUnits = await RetrieveResourceGroups();

            logger.FluentAddValue("TaskCountResourceUnits", capacityUnits.Count().ToString());

            // Run through found resource groups
            foreach (var resourceUnit in capacityUnits)
            {
                // Spawn out the tasks and run in parallel
                TaskHelper.RunBackground(
                    $"{LogBaseName}_run_unit_check",
                    (childLogger) =>
                    {
                        return childLogger.OperationScopeAsync(
                            $"{LogBaseName}_run_unit_check",
                            () => CoreRunUnitAsync(resourceUnit, claimSpan, childLogger),
                            swallowException: true);
                    },
                    logger);
            }

            return !Disposed;
        }

        private async Task CoreRunUnitAsync(IAzureResourceGroup capacityUnit, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("TaskRunId", Guid.NewGuid())
                .FluentAddBaseValue("TaskResourceSubscription", capacityUnit.Subscription.SubscriptionId)
                .FluentAddBaseValue("TaskResourceResourceGroup", capacityUnit.ResourceGroup);

            // Obtain a lease if no one else has it
            using (var lease = await ObtainLease($"{LeaseBaseName}-{capacityUnit.Subscription.SubscriptionId}-{capacityUnit.ResourceGroup}", claimSpan, logger))
            {
                logger.FluentAddValue("LeaseNotFound", lease == null);

                // If we couldn't obtain a lease, move on
                if (lease == null)
                {
                    return;
                }

                // Executes the action that needs to be performed on the pool
                await logger.TrackDurationAsync(
                    "RunPoolAction", () => RunActionAsync(capacityUnit, logger));
            }
        }

        private async Task RunActionAsync(IAzureResourceGroup capacityUnit, IDiagnosticsLogger logger)
        {
            var count = 0;
            var records = default(IPage<GenericResourceInner>);
            do
            {
                var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(capacityUnit.Subscription.SubscriptionId));

                // Get data page at a time
                records = records == null ?
                    await azure.Resources.ListByResourceGroupAsync(capacityUnit.ResourceGroup)
                    : await azure.Resources.ListByResourceGroupNextAsync(records.NextPageLink);

                logger.FluentAddValue("TaskFoundItems", count += records.Count());

                // Find the distinct ResourceIds
                var resources = records.Where(x => x.Tags != null
                        && x.Tags.ContainsKey(ResourceTagName.ResourceId)
                        && x.Tags.ContainsKey(ResourceTagName.ResourceType)
                        && x.Tags.ContainsKey(ResourceTagName.ResourceName))
                    .Select(x => (x.Tags, x.Tags[ResourceTagName.ResourceType], x.Tags[ResourceTagName.ResourceId], x.Tags[ResourceTagName.ResourceName], x.Location))
                    .Distinct();

                foreach (var resource in resources)
                {
                    await logger.OperationScopeAsync(
                        $"{LogBaseName}_run_orphaned_check",
                        () => RunOrphanedCheckAsync(resource, capacityUnit, logger.WithValues(new LogValueSet())),
                        swallowException: true);

                    // Need to slow things down so we don't blow the RUs
                    await Task.Delay(100);
                }
            }
            while (!string.IsNullOrEmpty(records.NextPageLink));
        }

        private async Task RunOrphanedCheckAsync((IDictionary<string, string> ResourceTags, string Type, string Id, string Name, string Location) resource, IAzureResourceGroup capacityUnit, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ResourceId", resource.Id)
                .FluentAddBaseValue("ResourceType", resource.Type);

            // Get record so we can tell if it exists
            var record = await ResourceRepository.GetAsync(resource.Id, logger.WithValues(new LogValueSet()));

            logger.FluentAddValue("ResourceExists", record != null);

            // If it doesn't exist, trigger delete
            if (record == null)
            {
                logger.LogWarning($"{LogBaseName}_stale_resource_found");

                if (!Enum.TryParse(resource.Type, false, out ResourceType resourceType))
                {
                    throw new NotSupportedException($"Resource has a tag of {resource.Type} which is not supported");
                }

                if (!Enum.TryParse(resource.Location, true, out AzureLocation resourceLocation))
                {
                    throw new NotSupportedException($"Resource has a location of {resource.Location} which is not supported");
                }

                if (resourceType == ResourceType.ComputeVM)
                {
                    await ContinuationTaskActivator.DeleteOrphanedComputeAsync(
                        Guid.Parse(resource.Id),
                        Guid.Parse(capacityUnit.Subscription.SubscriptionId),
                        capacityUnit.ResourceGroup,
                        resource.Name,
                        resourceLocation,
                        resource.ResourceTags,
                        logger.WithValues(new LogValueSet()));
                }
                else if (resourceType == ResourceType.StorageFileShare)
                {
                    await ContinuationTaskActivator.DeleteOrphanedStorageAsync(
                        Guid.Parse(resource.Id),
                        Guid.Parse(capacityUnit.Subscription.SubscriptionId),
                        capacityUnit.ResourceGroup,
                        resource.Name,
                        logger.WithValues(new LogValueSet()));
                }
            }
        }

        private async Task<IEnumerable<IAzureResourceGroup>> RetrieveResourceGroups()
        {
            var capacityUnits = (await CapacityManager.SelectAllAzureResourceGroups(AzureClientFactory)).Shuffle();
            return capacityUnits;
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
