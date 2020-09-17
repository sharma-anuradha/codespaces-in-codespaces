// <copyright file="WatchOrphanedAzureResourceTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
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
        private readonly Dictionary<string, string> apiVersionCache = new Dictionary<string, string>();

        private static readonly string[] DefaultEnabledResourceTypesToDelete = new[]
        {
            "Microsoft.Compute/virtualMachines",
            "Microsoft.Network/networkInterfaces",
            "Microsoft.Network/networkSecurityGroups",
            "Microsoft.Network/virtualNetworks",
            "Microsoft.KeyVault/vaults",
        };
        private static readonly string DefaultEnabledResourceTypesToDeleteConfigValue = string.Join(",", DefaultEnabledResourceTypesToDelete);

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
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedAzureResourceTask(
            ResourceBrokerSettings resourceBrokerSettings,
            IResourceRepository resourceRepository,
            IResourceContinuationOperations resourceContinuationOperations,
            ITaskHelper taskHelper,
            ICapacityManager capacityManager,
            IClaimedDistributedLease claimedDistributedLease,
            IAzureClientFactory azureClientFactory,
            IResourceNameBuilder resourceNameBuilder,
            IConfigurationReader configurationReader)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  capacityManager,
                  claimedDistributedLease,
                  resourceNameBuilder,
                  configurationReader)
        {
            AzureClientFactory = azureClientFactory;
            ResourceContinuationOperations = resourceContinuationOperations;
            ResourceRepository = resourceRepository;
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedAzureResourceTask);

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedAzureResourceTask";

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedAzureResourceTask;

        private IAzureClientFactory AzureClientFactory { get; }

        private IResourceContinuationOperations ResourceContinuationOperations { get; }

        private IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected override async Task ProcessResourceGroupAsync(IAzureResourceGroup resourceGroup, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_unit_check",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue("SubscriptionId", resourceGroup.Subscription.SubscriptionId)
                        .FluentAddBaseValue("ResourceGroup", resourceGroup.ResourceGroup);

                    var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(resourceGroup.Subscription.SubscriptionId));

                    var (resourcesById, resourcesWithoutId) = await FetchAzureResourcesAsync(azure, resourceGroup.ResourceGroup, childLogger.NewChildLogger());

                    foreach (var group in resourcesById)
                    {
                        await RunOrphanedCheckAsync(azure, group.Key, group.Value, childLogger.NewChildLogger());

                        // Slow down for Database RUs
                        await Task.Delay(LoopDelay);
                    }

                    childLogger
                        .LogInfo($"{LogBaseName}_run_handle_by_resourceid_complete");

                    foreach (var resource in resourcesWithoutId)
                    {
                        // There are some resources which don't have tags (either correctly or incorrectly) which
                        // we can't confidently just delete. Log them here instead to track.

                        childLogger
                            .FluentAddValue("AzureResourceId", resource.Id)
                            .FluentAddValue("AzureResourceType", resource.Type)
                            .FluentAddValue("ResourceDeleteAttempted", false)
                            .FluentAddValue("ResourceExists", "unknown")
                            .LogInfo($"{LogBaseName}_unknown_resource");
                    }

                    childLogger
                        .LogInfo($"{LogBaseName}_run_handle_unknown_complete");
                });
        }

        private async Task<(Dictionary<string, List<GenericResourceInner>> ResourcesById, List<GenericResourceInner> ResourcesWithoutId)> FetchAzureResourcesAsync(IResourceManagementClient azure, string resourceGroup, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_run_fetch",
                async (childLogger) =>
                {
                    var resourcesById = new Dictionary<string, List<GenericResourceInner>>();
                    var resourcesWithoutId = new List<GenericResourceInner>();

                    var count = 0;
                    var page = 0;
                    var records = await azure.Resources.ListByResourceGroupAsync(resourceGroup);

                    while (records.Any())
                    {
                        AggregateResourcePage(records, resourcesById, resourcesWithoutId);

                        count += records.Count();
                        page++;

                        if (string.IsNullOrWhiteSpace(records.NextPageLink))
                        {
                            break;
                        }

                        records = await azure.Resources.ListByResourceGroupNextAsync(records.NextPageLink);
                    }

                    childLogger
                        .FluentAddValue("FoundItems", count)
                        .FluentAddValue("FoundPage", page)
                        .FluentAddValue("ResourcesWithIds", resourcesById.Count)
                        .FluentAddValue("ResourcesWithoutIds", resourcesWithoutId.Count);

                    return (resourcesById, resourcesWithoutId);
                },
                swallowException: false);
        }

        private void AggregateResourcePage(
            IPage<GenericResourceInner> page,
            Dictionary<string, List<GenericResourceInner>> resourcesById,
            List<GenericResourceInner> resourcesWithoutId)
        {
            foreach (var resource in page)
            {
                if (resource.Tags == null || !resource.Tags.TryGetValue(ResourceTagName.ResourceId, out var resourceId) || resourceId == null)
                {
                    resourcesWithoutId.Add(resource);
                    continue;
                }

                if (!resourcesById.TryGetValue(resourceId, out var resourcesForId))
                {
                    resourcesById[resourceId] = resourcesForId = new List<GenericResourceInner>();
                }

                resourcesForId.Add(resource);
            }
        }

        private async Task RunOrphanedCheckAsync(IResourceManagementClient azure, string resourceId, List<GenericResourceInner> azureResources, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_orphaned_check",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, resourceId);

                    // Get record so we can tell if it exists
                    var record = await ResourceRepository.GetAsync(resourceId, childLogger.NewChildLogger());

                    await Retry.DoAsync(
                        async (int attemptNumber) =>
                        {
                            childLogger.FluentAddBaseValue("ResourceExists", record != null);
                            childLogger.AddAttempt(attemptNumber);

                            // If it doesn't exist, trigger delete, otherwise update keepalive
                            if (record == null)
                            {
                                foreach (var resource in azureResources)
                                {
                                    await DeleteResourceAsync(azure, resource, childLogger.NewChildLogger());
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
                        async (int attemptNumber, Exception ex) =>
                        {
                            childLogger.AddAttempt(attemptNumber);

                            if (ex is DocumentClientException dcex && dcex.StatusCode == HttpStatusCode.PreconditionFailed)
                            {
                                record = await ResourceRepository.GetAsync(resourceId, childLogger.NewChildLogger());
                            }
                        });
                },
                swallowException: true);
        }

        private async Task DeleteResourceAsync(IResourceManagementClient azure, GenericResourceInner azureResource, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_delete_orphan",
                async (childLogger) =>
                {
                    var canDeleteResource = await CanDeleteResourceType(azureResource.Type, childLogger.NewChildLogger());

                    childLogger
                        .FluentAddBaseValue("AzureResourceType", azureResource.Type)
                        .FluentAddBaseValue("AzureResourceId", azureResource.Id)
                        .FluentAddBaseValue("ResourceLocation", azureResource.Location)
                        .FluentAddBaseValue("ResourceDeleteAttempted", canDeleteResource);

                    if (canDeleteResource)
                    {
                        var apiVersion = await GetApiVersionForResourceTypeAsync(azure, azureResource.Type);
                        await azure.Resources.BeginDeleteByIdAsync(azureResource.Id, apiVersion);
                    }
                },
                swallowException: true);
        }

        private async Task<bool> CanDeleteResourceType(string resourceType, IDiagnosticsLogger logger)
        {
            var enabledResourceTypes = await this.ConfigurationReader.ReadSettingAsync(ConfigurationBaseName, "enabled-resource-types", logger.NewChildLogger(), DefaultEnabledResourceTypesToDeleteConfigValue);
            if (enabledResourceTypes == default)
            {
                // No resource types are enabled
                return false;
            }

            var enabledResourceTypeList = enabledResourceTypes.Split(',').Select(type => type.Trim());

            var isEnabled = enabledResourceTypeList.Any((enabledType) => string.Equals(enabledType, resourceType, StringComparison.OrdinalIgnoreCase));

            return isEnabled;
        }

        private async Task<string> GetApiVersionForResourceTypeAsync(IResourceManagementClient azure, string resourceType)
        {
            if (apiVersionCache.TryGetValue(resourceType, out var apiVersion))
            {
                return apiVersion;
            }

            // Format is "providerName/kind" - e.g. Microsoft.Compute/virtualMachines
            var parts = resourceType.Split('/');
            if (parts.Length != 2)
            {
                throw new FormatException($"Azure resource type in unexpected format: {resourceType}");
            }

            var providerName = parts[0];

            var provider = await azure.Providers.GetAsync(providerName);
            if (provider == null)
            {
                throw new NotFoundException($"Azure Provider not found: {providerName}");
            }

            // Cache all resource types in the provider as we use many types with the same provider
            var apiVersionsForTypes = provider.ResourceTypes
                .Select((type) => ($"{providerName}/{type.ResourceType}", type.ApiVersions.OrderByDescending(v => v).FirstOrDefault()))
                .Where((pair) => !string.IsNullOrEmpty(pair.Item2));

            foreach (var (type, version) in apiVersionsForTypes)
            {
                apiVersionCache[type] = version;
            }

            if (apiVersionCache.TryGetValue(resourceType, out apiVersion))
            {
                return apiVersion;
            }

            // Provider was found, but couldn't find this specific type in it
            throw new NotFoundException($"Azure resource type not found in Provider: {resourceType}");
        }
    }
}
