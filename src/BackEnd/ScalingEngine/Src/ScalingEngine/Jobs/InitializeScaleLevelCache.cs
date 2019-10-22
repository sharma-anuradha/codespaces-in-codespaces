// <copyright file="InitializeScaleLevelCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine.Jobs
{
    /// <summary>
    /// Initializes Scale Level Cache.
    /// </summary>
    public class InitializeScaleLevelCache : IAsyncWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeScaleLevelCache"/> class.
        /// </summary>
        /// <param name="systemCatalog">System Catalog that refines all the current resource.</param>
        /// <param name="resourceScalingBroker">Resource Scaling Broker that will be notified
        /// once scaling levels have been computed.</param>
        public InitializeScaleLevelCache(
            ISystemCatalog systemCatalog,
            IResourceScalingHandler resourceScalingBroker)
        {
            SystemCatalog = systemCatalog;
            ResourceScalingBroker = resourceScalingBroker;
        }

        private ISystemCatalog SystemCatalog { get; }

        private IResourceScalingHandler ResourceScalingBroker { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            var resourceSkus = FlattenResourceSkus(SystemCatalog.SkuCatalog.CloudEnvironmentSkus.Values);

            await ResourceScalingBroker.UpdateResourceScaleLevels(new ScalingInput() { Pools = resourceSkus });
        }

        private IEnumerable<ResourcePool> FlattenResourceSkus(IEnumerable<ICloudEnvironmentSku> cloudEnvironmentSku)
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
                        "environmentSkus": [
                            "Small-Linux-Preview"
                        ]
                    },
                    {
                        "type": "compute",
                        "skuName": "Standard_F4s_v2",
                        "location": "eastus",
                        "targetCount": 10,
                        "environmentSkus": [
                            "Small-Linux-Preview"
                        ]
                    },
                    {
                        "type": "compute",
                        "skuName": "Standard_F8s_v2",
                        "location": "westus2",
                        "targetCount": 10,
                        "environmentSkus": [
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
                        "environmentSkus": [
                            "Small-Linux-Preview",
                            "Medium-Linux-Preview"
                        ]
                    },
                    {
                        "type": "storage",
                        "skuName": "Premium_LRS",
                        "location": "eastus",
                        "targetCount": 10,
                        "environmentSkus": [
                            "Medium-Linux-Preview"
                        ]
                    },
                ]
            */

            // Flatten out list so we have one sku per target region
            var flatEnvironmentSkus = cloudEnvironmentSku
                .Where(s => !s.IsExternalHardware)
                .SelectMany(x => x.SkuLocations
                    .Select(y => new FlatComputeItem
                    {
                        Location = y,
                        Environment = x,
                        ComputeDetails = new ResourcePoolComputeDetails
                        {
                            OS = x.ComputeOS,
                            ImageFamilyName = x.ComputeImage.ImageFamilyName,
                            ImageName = x.ComputeImage.GetCurrentImageUrl(y),
                            VmAgentImageName = x.VmAgentImage.ImageName,
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
                            ImageName = x.StorageImage.ImageName,
                            Location = y,
                            SkuName = x.StorageSkuName,
                        },
                    }));

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
            var resourceSkus = new List<ResourcePool>(storageSkus).Concat(computeSkus);

            return resourceSkus;
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
                EnvironmentSkus = distinctList.Select(y => y.Environment.SkuName),
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
