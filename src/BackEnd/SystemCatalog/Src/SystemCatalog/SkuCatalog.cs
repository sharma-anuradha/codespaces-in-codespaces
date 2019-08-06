// <copyright file="SkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
{
    /// <summary>
    /// A SKU catalog based on <see cref="SkuCatalogSettings"/> from AppSettings.
    /// </summary>
    public class SkuCatalog : ISkuCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCatalog"/> class.
        /// </summary>
        /// <param name="skuCatalogOptions">The options instance for the sku catalog.</param>
        public SkuCatalog(IOptions<SkuCatalogOptions> skuCatalogOptions)
            : this(skuCatalogOptions.Value.Settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCatalog"/> class.
        /// </summary>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        public SkuCatalog(SkuCatalogSettings skuCatalogSettings)
        {
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));

            // Create the ordered, immutable list, same for all configured subscriptions.
            var defaultLocations = new ReadOnlyCollection<AzureLocation>(skuCatalogSettings.DefaultLocations
                .Distinct()
                .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                .ToList());

            foreach (var cloudEnvironmentSettings in skuCatalogSettings.CloudEnvironmentSkuSettings)
            {
                var key = cloudEnvironmentSettings.ComputeSkuName;
                if (CloudEnvironmentSkuDictionary.ContainsKey(key))
                {
                    throw new InvalidOperationException($"The cloud environment sku '{key}' already exists.");
                }

                var computeOS = cloudEnvironmentSettings.ComputeOS;
                if (!skuCatalogSettings.DefaultVMImages.TryGetValue(computeOS, out string defaultVMImage))
                {
                    throw new InvalidOperationException($"A {nameof(skuCatalogSettings.DefaultVMImages)} entry is required for OS '{computeOS}'.");
                }

                var skuLocations = defaultLocations;
                if (cloudEnvironmentSettings.OverrideLocations.Any())
                {
                    skuLocations = new ReadOnlyCollection<AzureLocation>(cloudEnvironmentSettings.OverrideLocations
                        .Distinct()
                        .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                        .ToList());
                }

                var cloudEnvironment = new CloudEnvironmentSku(
                    cloudEnvironmentSettings.SkuName,
                    cloudEnvironmentSettings.SkuDisplayName,
                    skuLocations,
                    cloudEnvironmentSettings.ComputeSkuFamily,
                    cloudEnvironmentSettings.ComputeSkuName,
                    cloudEnvironmentSettings.ComputeSkuSize,
                    computeOS,
                    defaultVMImage,
                    cloudEnvironmentSettings.StorageSkuName,
                    cloudEnvironmentSettings.StorageSizeInGB,
                    cloudEnvironmentSettings.StorageCloudEnvironmentUnits,
                    cloudEnvironmentSettings.ComputeCloudEnvironmentUnits);

                CloudEnvironmentSkuDictionary.Add(key, cloudEnvironment);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ICloudEnvironmentSku> CloudEnvironmentSkus => CloudEnvironmentSkuDictionary.Values;

        private Dictionary<string, ICloudEnvironmentSku> CloudEnvironmentSkuDictionary { get; } = new Dictionary<string, ICloudEnvironmentSku>();

        /// <inheritdoc/>
        public bool TryGetCloudEnvironmentSku(string skuName, out ICloudEnvironmentSku cloudEnvironmentSku)
        {
            return CloudEnvironmentSkuDictionary.TryGetValue(skuName, out cloudEnvironmentSku);
        }
    }
}
