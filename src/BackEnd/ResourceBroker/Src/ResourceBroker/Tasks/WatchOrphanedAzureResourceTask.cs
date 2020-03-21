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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned Azure Resources.
    /// </summary>
    public class WatchOrphanedAzureResourceTask : BaseDataPlaneResourceGroupTask, IWatchOrphanedAzureResourceTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedAzureResourceTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="resourceContinuationOperations">Target continuation task activator.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="capacityManager">Target capacity manager.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        public WatchOrphanedAzureResourceTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper,
            ICapacityManager capacityManager,
            IClaimedDistributedLease claimedDistributedLease,
            IAzureClientFactory azureClientFactory,
            IResourceNameBuilder resourceNameBuilder)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  capacityManager,
                  claimedDistributedLease,
                  resourceNameBuilder)
        {
            AzureClientFactory = azureClientFactory;
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedAzureResourceTask);

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedAzureResourceTask;

        private IAzureClientFactory AzureClientFactory { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected override async Task ProcessResourceGroupAsync(IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger)
        {
            var count = 0;
            var page = 0;
            var records = default(IPage<GenericResourceInner>);

            do
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_run_unit_check_page",
                    async (childLogger) =>
                    {
                        var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(resourceGroup.Subscription.SubscriptionId));

                        // Get data page at a time
                        records = records == null ?
                            await azure.Resources.ListByResourceGroupAsync(resourceGroup.ResourceGroup)
                            : await azure.Resources.ListByResourceGroupNextAsync(records.NextPageLink);

                        childLogger.FluentAddValue("TaskFoundItems", count += records.Count())
                            .FluentAddValue("TaskFoundPage", page++);

                        // Find the distinct ResourceIds
                        var resources = records.Where(x => x.Tags != null
                                && x.Tags.ContainsKey(ResourceTagName.ResourceId)
                                && x.Tags.ContainsKey(ResourceTagName.ResourceType)
                                && x.Tags.ContainsKey(ResourceTagName.ResourceName))
                            .Select(x => (x.Tags, x.Tags[ResourceTagName.ResourceType], x.Tags[ResourceTagName.ResourceId], x.Tags[ResourceTagName.ResourceName], x.Location))
                            .Distinct();

                        foreach (var resource in resources)
                        {
                            await RunOrphanedCheckAsync(resource, resourceGroup, childLogger);

                            // Slow down for Database RUs
                            await Task.Delay(LoopDelay);
                        }
                    });
            }
            while (!string.IsNullOrEmpty(records.NextPageLink));
        }

        private Task RunOrphanedCheckAsync((IDictionary<string, string> ResourceTags, string Type, string Id, string Name, string Location) resource, IAzureResourceGroup capacityUnit, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_orphaned_check",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resource.Id)
                        .FluentAddBaseValue("ResourceType", resource.Type);

                    // Get record so we can tell if it exists
                    var record = await ResourceRepository.GetAsync(resource.Id, childLogger.NewChildLogger());

                    childLogger.FluentAddValue("ResourceExists", record != null);

                    // If it doesn't exist, trigger delete, otherwise update keepalive
                    if (record == null)
                    {
                        if (!Enum.TryParse(resource.Type, false, out ResourceType resourceType))
                        {
                            throw new NotSupportedException($"Resource has a tag of {resource.Type} which is not supported");
                        }

                        if (!Enum.TryParse(resource.Location, true, out AzureLocation resourceLocation))
                        {
                            throw new NotSupportedException($"Resource has a location of {resource.Location} which is not supported");
                        }

                        // Now we can delete the azure resource
                        if (resourceType == ResourceType.ComputeVM)
                        {
                            await ResourceContinuationOperations.DeleteOrphanedComputeAsync(
                                Guid.Parse(resource.Id),
                                Guid.Parse(capacityUnit.Subscription.SubscriptionId),
                                capacityUnit.ResourceGroup,
                                resource.Name,
                                resourceLocation,
                                resource.ResourceTags,
                                "OrphanedAzureResourceTask",
                                childLogger.NewChildLogger());
                        }
                        else if (resourceType == ResourceType.StorageFileShare
                            || resourceType == ResourceType.StorageArchive)
                        {
                            await ResourceContinuationOperations.DeleteOrphanedStorageAsync(
                                Guid.Parse(resource.Id),
                                Guid.Parse(capacityUnit.Subscription.SubscriptionId),
                                capacityUnit.ResourceGroup,
                                resource.Name,
                                resourceLocation,
                                resource.ResourceTags,
                                "OrphanedAzureResourceTask",
                                childLogger.NewChildLogger());
                        }
                    }
                    else
                    {
                        // Update datetime
                        record.KeepAlives.AzureResourceAlive = DateTime.UtcNow;

                        // Update database record
                        await ResourceRepository.UpdateAsync(record, childLogger.NewChildLogger());
                    }
                },
                swallowException: true);
        }
    }
}
