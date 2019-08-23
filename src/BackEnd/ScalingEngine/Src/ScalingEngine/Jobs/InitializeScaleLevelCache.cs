// <copyright file="InitializeScaleLevelCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

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
            IResourceScalingBroker resourceScalingBroker)
        {
            SystemCatalog = systemCatalog;
            ResourceScalingBroker = resourceScalingBroker;
        }

        private ISystemCatalog SystemCatalog { get; }

        private IResourceScalingBroker ResourceScalingBroker { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync()
        {
            var resourceSkus = FlattenResourceSkus(SystemCatalog.SkuCatalog.CloudEnvironmentSkus);

            await ResourceScalingBroker.UpdateResourceScaleLevels(resourceSkus);
        }

        private IList<ScalingInput> FlattenResourceSkus(IEnumerable<ICloudEnvironmentSku> cloudEnvironmentSku)
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
                .SelectMany(x => x.SkuLocations
                    .Select(y => new FlatComputeItem { Location = y, Environment = x }));

            // Calculate the distinct storage skus that are needed in each region
            var storageSkus = flatEnvironmentSkus
                .GroupBy(x => new { SkuName = x.Environment.StorageSkuName, Location = x.Location })
                .Select(x => BuildScalingInput(x, ResourceType.StorageFileShare, y => y.Environment.StorageSkuName));

            // Calculate the distinct compute skus that are needed in each region
            var computeSkus = flatEnvironmentSkus
                .GroupBy(x => new { SkuName = x.Environment.ComputeSkuName, Location = x.Location })
                .Select(x => BuildScalingInput(x, ResourceType.ComputeVM, y => y.Environment.ComputeSkuName));

            // Merge lists back together
            var resourceSkus = new List<ScalingInput>(storageSkus).Concat(computeSkus).ToList();

            return resourceSkus;
        }

        private ScalingInput BuildScalingInput(
            IEnumerable<FlatComputeItem> distinctList,
            ResourceType resourceType,
            Func<FlatComputeItem, string> skuNameCallback)
        {
            var environments = distinctList.Select(y => y.Environment);
            var target = distinctList.FirstOrDefault();
            return new ScalingInput
            {
                TargetCount = distinctList.Select(y => y.Environment.PoolLevel).Sum(),
                SkuName = skuNameCallback(target),
                Location = target.Location.ToString().ToLowerInvariant(),
                Type = resourceType,
                EnvironmentSkus = distinctList.Select(y => y.Environment.SkuName),
            };
        }

        private class FlatComputeItem
        {
            public AzureLocation Location { get; set; }

            public ICloudEnvironmentSku Environment { get; set; }
        }
    }
}
