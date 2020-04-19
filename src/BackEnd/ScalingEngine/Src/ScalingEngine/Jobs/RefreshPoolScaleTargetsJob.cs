// <copyright file="RefreshPoolScaleTargetsJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Jobs
{
    /// <summary>
    /// Background job that refreshes the pool scale target sizes and configurations.
    /// If the configuration changes while the service is running (either target size or count),
    /// then this job will pick it up and set the pool scale levels accordingly.
    /// </summary>
    public class RefreshPoolScaleTargetsJob : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshPoolScaleTargetsJob"/> class.
        /// </summary>
        /// <param name="systemCatalog">System Catalog that refines all the current resource.</param>
        /// <param name="resourceScalingBroker">Resource Scaling Broker that will be notified
        /// once scaling levels have been computed.</param>
        /// <param name="resourcePoolSettingsRepository">The settings repository where current settings
        /// overrides can be configured.</param>
        /// <param name="taskHelper">The task helper for triggering background jobs.</param>
        /// <param name="azureSubscriptionCatalogOptions">The azure subscription catalog options.</param>
        public RefreshPoolScaleTargetsJob(
            ISystemCatalog systemCatalog,
            IResourceScalingHandler resourceScalingBroker,
            IResourcePoolSettingsRepository resourcePoolSettingsRepository,
            ITaskHelper taskHelper,
            IOptions<AzureSubscriptionCatalogOptions> azureSubscriptionCatalogOptions)
        {
            SystemCatalog = systemCatalog;
            ResourceScalingBroker = resourceScalingBroker;
            ResourcePoolSettingsRepository = resourcePoolSettingsRepository;
            TaskHelper = taskHelper;
            DataPlaneSettings = azureSubscriptionCatalogOptions.Value.DataPlaneSettings;
        }

        private ISystemCatalog SystemCatalog { get; }

        private IResourceScalingHandler ResourceScalingBroker { get; }

        private IResourcePoolSettingsRepository ResourcePoolSettingsRepository { get; }

        private ITaskHelper TaskHelper { get; }

        private DataPlaneSettings DataPlaneSettings { get; }

        /// <inheritdoc/>
        public Task WarmupCompletedAsync()
        {
            // Kick off background job that re-evaluates the catalog every minute
            return TaskHelper.RunBackgroundLoopAsync(
                $"{nameof(RefreshPoolScaleTargetsJob)}_run",
                (childLogger) => UpdateScaleLevelsAsync(childLogger),
                TimeSpan.FromMinutes(1));
        }

        private async Task<bool> UpdateScaleLevelsAsync(IDiagnosticsLogger logger)
        {
            var pools = new List<ResourcePool>();

            var environmentSkus = await FlattenEnvironmentSkusAsync(SystemCatalog.SkuCatalog.CloudEnvironmentSkus.Values, logger);
            pools.AddRange(environmentSkus);

            var keyVaultPools = GetKeyVaultPools(logger);
            pools.AddRange(keyVaultPools);

            await ResourceScalingBroker.UpdateResourceScaleLevels(new ScalingInput() { Pools = pools });
            return true;
        }

        private IList<ResourcePool> GetKeyVaultPools(IDiagnosticsLogger logger)
        {
            logger.LogInfo($"{GetType().FormatLogMessage(nameof(GetKeyVaultPools))}");

            var keyVaultPools = new List<ResourcePool>();
            foreach (var location in DataPlaneSettings.DefaultLocations)
            {
                var standardKeyVaultPoolSku = ConstructKeyVaultPool(location);
                keyVaultPools.Add(standardKeyVaultPoolSku);
            }

            return keyVaultPools;
        }

        private ResourcePool ConstructKeyVaultPool(AzureLocation location)
        {
            const string KeyVaultAzureSkuName = "Standard";
            const string logicalPlanSkuName = "standardPlan";
            const int KeyVaultTargetPoolSize = 10;

            var resourcePoolKeyVaultDetails = new ResourcePoolKeyVaultDetails
            {
                SkuName = KeyVaultAzureSkuName,
                Location = location,
            };
            var keyVaultSku = new ResourcePool
            {
                Details = resourcePoolKeyVaultDetails,
                LogicalSkus = new string[] { logicalPlanSkuName },
                Id = resourcePoolKeyVaultDetails.GetPoolDefinition(),
                IsEnabled = true,
                TargetCount = KeyVaultTargetPoolSize,
                Type = ResourceType.KeyVault,
            };

            return keyVaultSku;
        }

        private async Task<IList<ResourcePool>> FlattenEnvironmentSkusAsync(
            IEnumerable<ICloudEnvironmentSku> cloudEnvironmentSku,
            IDiagnosticsLogger logger)
        {
            /*
            Phase 0 (input):
                [
                    {
                        "skuName": "Small-Linux-Preview",
                        "computeSkuName": "Standard_F4s_v2",
                        "storageSkuName": "Premium_LRS",
                        "overridePoolLevel": 10,
                        "locations": [
                            "westus2",
                            "eastus"
                        ]
                    },
                    {
                        "skuName": "Medium-Linux-Preview",
                        "computeSkuName": "Standard_F8s_v2",
                        "storageSkuName": "Premium_LRS",
                        "overridePoolLevel": 10,
                        "locations": [
                            "westus2"
                        ]
                    }
                ]

            Phase 1 (flatten):
                [
                    {
                        "location": "westus2",
                        "environment": {
                            "skuName": "Small-Linux-Preview",
                            "computeSkuName": "Standard_F4s_v2",
                            "storageSkuName": "Premium_LRS",
                            "overridePoolLevel": 10,
                            "locations": [
                            "westus2",
                            "eastus"
                            ]
                        },
                    },
                    {
                        "location": "eastus",
                        "environment": {
                            "skuName": "Small-Linux-Preview",
                            "computeSkuName": "Standard_F4s_v2",
                            "storageSkuName": "Premium_LRS",
                            "overridePoolLevel": 10,
                            "locations": [
                            "westus2",
                            "eastus"
                            ]
                        },
                    },
                    {
                        "location": "westus2",
                        "environment":{
                            "skuName": "Medium-Linux-Preview",
                            "computeSkuName": "Standard_F8s_v2",
                            "storageSkuName": "Premium_LRS",
                            "overridePoolLevel": 10,
                            "locations": [
                            "westus2"
                            ]
                        }
                    }
                ]

            Phase 2 (distinct and grouping):
                // COMPUTE
                [
                    {
                        "type": "compute",
                        "skuName": "Standard_F4s_v2",
                        "location": "westus2",
                        "targetCount": 10,
                        "logicalSkus": [
                            "Small-Linux-Preview"
                        ]
                    },
                    {
                        "type": "compute",
                        "skuName": "Standard_F4s_v2",
                        "location": "eastus",
                        "targetCount": 10,
                        "logicalSkus": [
                            "Small-Linux-Preview"
                        ]
                    },
                    {
                        "type": "compute",
                        "skuName": "Standard_F8s_v2",
                        "location": "westus2",
                        "targetCount": 10,
                        "logicalSkus": [
                            "Medium-Linux-Preview"
                        ]
                    },
                ]
                // STORAGE
                [
                    {
                        "type": "storage",
                        "skuName": "Premium_LRS",
                        "location": "westus2",
                        "targetCount": 20,
                        "logicalSkus": [
                            "Small-Linux-Preview",
                            "Medium-Linux-Preview"
                        ]
                    },
                    {
                        "type": "storage",
                        "skuName": "Premium_LRS",
                        "location": "eastus",
                        "targetCount": 10,
                        "logicalSkus": [
                            "Medium-Linux-Preview"
                        ]
                    },
                ]
            */

            // Flatten out list so we have one sku per target region
            var flatEnvironmentSkus = await Task.WhenAll(cloudEnvironmentSku
                .Where(s => !s.IsExternalHardware)
                .SelectMany(x => x.SkuLocations
                    .Select(async y => new FlatComputeItem
                    {
                        Location = y,
                        Environment = x,
                        ComputeDetails = new ResourcePoolComputeDetails
                        {
                            OS = x.ComputeOS,
                            ImageFamilyName = x.ComputeImage.ImageFamilyName,
                            ImageName = await x.ComputeImage.GetCurrentImageUrlAsync(y, logger.NewChildLogger()),
                            VmAgentImageName = await x.VmAgentImage.GetCurrentImageNameAsync(logger.NewChildLogger()),
                            VmAgentImageFamilyName = x.VmAgentImage.ImageFamilyName,
                            Location = y,
                            SkuName = x.ComputeSkuName,
                            SkuFamily = x.ComputeSkuFamily,
                            Cores = x.ComputeSkuCores,
                        },
                        StorageDetails = new ResourcePoolStorageDetails
                        {
                            SizeInGB = x.StorageSizeInGB,
                            ImageFamilyName = x.StorageImage.ImageFamilyName,
                            ImageName = await x.StorageImage.GetCurrentImageNameAsync(logger.NewChildLogger()),
                            Location = y,
                            SkuName = x.StorageSkuName,
                        },
                    })));

            // Calculate the distinct storage skus that are needed in each region
            var storageSkusGroup = flatEnvironmentSkus
                .GroupBy(x => x.StorageDetails.GetPoolDefinition());
            var storageSkus = storageSkusGroup.Select(
                x => BuildScalingInput(x, ResourceType.StorageFileShare, y => y.StorageDetails, y => y.Environment.StoragePoolLevel));

            // Calculate the distinct compute skus that are needed in each region
            var computeSkusGroup = flatEnvironmentSkus
                .GroupBy(x => x.ComputeDetails.GetPoolDefinition());
            var computeSkus = computeSkusGroup.Select(
                x => BuildScalingInput(x, ResourceType.ComputeVM, y => y.ComputeDetails, y => y.Environment.ComputePoolLevel));

            // Merge lists back together
            var resourceSkus = new List<ResourcePool>(storageSkus).Concat(computeSkus).ToList();

            // Check for some pool settings overrides in the database
            // TODO: Migrate these to SystemConfiguration, similar to how the image name/versions are overridden
            await OverridePoolSettingsAsync(resourceSkus, logger);

            return resourceSkus;
        }

        private Task OverridePoolSettingsAsync(List<ResourcePool> pools, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{nameof(RefreshPoolScaleTargetsJob)}_override_settings",
                async (childLogger) =>
                {
                    // Pull out core settings records
                    var settings = (await ResourcePoolSettingsRepository.GetWhereAsync(x => true, childLogger.NewChildLogger()))
                        .ToDictionary(x => x.Id);

                    childLogger.FluentAddValue("SettingsFoundCount", settings.Count)
                        .FluentAddValue("SettingsFoundData", JsonConvert.SerializeObject(
                            settings, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }));

                    childLogger.FluentAddValue("SettingsFoundPoolDefinitionCount", pools.Count());

                    // Run through each pool item
                    foreach (var pool in pools)
                    {
                        await childLogger.OperationScopeAsync(
                            $"{nameof(RefreshPoolScaleTargetsJob)}_override_settings_unit_check",
                            (itemLogger) =>
                            {
                                var poolDefinition = pool.Details.GetPoolDefinition();

                                itemLogger.FluentAddValue("SettingsFoundPool", poolDefinition)
                                    .FluentAddValue("SettingPreOverrideTargetCount", pool.OverrideTargetCount)
                                    .FluentAddValue("SettingPreOverrideIsEnabled", pool.OverrideIsEnabled);

                                // Overrides the value if we have settings for it
                                if (settings.TryGetValue(poolDefinition, out var resourceSetting))
                                {
                                    pool.OverrideTargetCount = resourceSetting.TargetCount;
                                    pool.OverrideIsEnabled = resourceSetting.IsEnabled;
                                }
                                else
                                {
                                    // Clear out any overrides if we don't have matches
                                    pool.OverrideTargetCount = null;
                                    pool.OverrideIsEnabled = null;
                                }

                                itemLogger.FluentAddValue("SettingPostOverrideTargetCount", pool.OverrideTargetCount)
                                    .FluentAddValue("SettingPostOverrideIsEnabled", pool.OverrideIsEnabled);

                                return Task.CompletedTask;
                            });
                    }
                },
                swallowException: true);
        }

        private ResourcePool BuildScalingInput(
            IEnumerable<FlatComputeItem> distinctList,
            ResourceType resourceType,
            Func<FlatComputeItem, ResourcePoolResourceDetails> detailCallback,
            Func<FlatComputeItem, int> poolLevelCallback)
        {
            var environments = distinctList.Select(y => y.Environment);
            var target = distinctList.FirstOrDefault();
            var details = detailCallback(target);
            return new ResourcePool
            {
                Id = details.GetPoolDefinition(),
                Type = resourceType,
                TargetCount = distinctList.Select(poolLevelCallback).Sum(),
                LogicalSkus = distinctList.Select(y => y.Environment.SkuName),
                Details = details,
            };
        }

        private class FlatComputeItem
        {
            public AzureLocation Location { get; set; }

            public ICloudEnvironmentSku Environment { get; set; }

            public ResourcePoolStorageDetails StorageDetails { get; set; }

            public ResourcePoolComputeDetails ComputeDetails { get; set; }
        }
    }
}
