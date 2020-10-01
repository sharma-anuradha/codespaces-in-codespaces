// <copyright file="WatchOrphanedAzureResourceJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Watches for Orphaned Azure Resources.
    /// </summary>
    public class WatchOrphanedAzureResourceJobHandler : BaseDataPlaneResourceGroupJobHandler<WatchOrphanedAzureResourceJobHandler>
    {
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
        /// Initializes a new instance of the <see cref="WatchOrphanedAzureResourceJobHandler"/> class.
        /// </summary>
        /// <param name="resourceRepository">Target resource repository.</param>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <param name="configurationReader">Configuration reader</param>
        /// <param name="jobQueueProducerFactory">Job queue producer factory.</param>
        public WatchOrphanedAzureResourceJobHandler(
            IResourceRepository resourceRepository,
            IAzureClientFactory azureClientFactory,
            IConfigurationReader configurationReader,
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            AzureClientFactory = azureClientFactory;
            ResourceRepository = resourceRepository;
            ConfigurationReader = configurationReader;
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(ResourceJobQueueConstants.GenericQueueName);
        }

        private string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedAzureResourceJobHandler;

        private IAzureClientFactory AzureClientFactory { get; }

        private IResourceRepository ResourceRepository { get; }

        private IConfigurationReader ConfigurationReader { get; }

        private IJobQueueProducer JobQueueProducer { get; }

        private string ConfigurationBaseName => "WatchOrphanedAzureResourceTask";

        /// <inheritdoc/>
        protected override async Task HandleJobAsync(string subscriptionId, string resourceGroupName, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_unit_check",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue("SubscriptionId", subscriptionId)
                        .FluentAddBaseValue("ResourceGroup", resourceGroupName);

                    var azure = await AzureClientFactory.GetResourceManagementClient(Guid.Parse(subscriptionId));

                    var (resourcesById, resourcesWithoutId) = await FetchAzureResourcesAsync(azure, resourceGroupName, childLogger.NewChildLogger());

                    foreach (var group in resourcesById)
                    {
                        await RunOrphanedCheckAsync(azure, group.Key, group.Value, childLogger.NewChildLogger());
                    }

                    childLogger
                        .LogInfo($"{LogBaseName}_run_handle_by_resourceid_complete");

                    foreach (var resource in resourcesWithoutId)
                    {
                        // There are some resources which don't have tags (either correctly or incorrectly) which
                        // we can't confidently just delete. Log them here instead to track.

                        childLogger.NewChildLogger()
                            .AddBaseAzureResource(resource)
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
                                    await DeleteResourceProducerAsync(azure, resource, childLogger.NewChildLogger());
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

        private async Task DeleteResourceProducerAsync(IResourceManagementClient azure, GenericResourceInner azureResource, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_delete_orphan_producer",
                async (childLogger) =>
                {
                    var canDeleteResource = await CanDeleteResourceType(azureResource.Type, childLogger.NewChildLogger());

                    childLogger
                        .AddBaseAzureResource(azureResource)
                        .FluentAddBaseValue("ResourceDeleteAttempted", canDeleteResource);

                    if (canDeleteResource)
                    {
                        // Create a job payload and append a set of properties from our child logger
                        var jobPayload = new DeleteAzureResourcePayload()
                        {
                            SubscriptionId = azure.SubscriptionId,
                            AzureResource = azureResource,
                        }.WithLoggerProperties(
                            childLogger,
                            "SubscriptionId",
                            "ResourceGroup",
                            "AzureResourceType",
                            "ResourceLocation",
                            "AzureResourceId",
                            "AzureResourceTags",
                            "ResourceId");
                        var jobPayloadOptions = new JobPayloadOptions()
                        {
                            ExpireTimeout = JobPayloadOptions.DefaultJobPayloadExpireTimeout,
                        };

                        var message = await JobQueueProducer.AddJobAsync(jobPayload, jobPayloadOptions, childLogger.NewChildLogger(), default);
                        childLogger.FluentAddBaseValue(JobQueueLoggerConst.JobId, message.Id);
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
    }
}
