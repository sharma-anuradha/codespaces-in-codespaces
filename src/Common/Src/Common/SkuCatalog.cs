// <copyright file="SkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
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
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            IOptions<SkuCatalogOptions> skuCatalogOptions,
            IControlPlaneInfo controlPlaneInfo)
            : this(skuCatalogOptions.Value.Settings, controlPlaneInfo)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCatalog"/> class.
        /// </summary>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        /// <param name="currentLocationProvider">The current azure location provider.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            SkuCatalogSettings skuCatalogSettings,
            IControlPlaneInfo controlPlaneInfo)
        {
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));

            // Get the supported azure locations
            var dataPlaneLocations = new HashSet<AzureLocation>(controlPlaneInfo.GetAllDataPlaneLocations());

            // Get the default configuration for all skus.
            var defaultSkuConfiguration = skuCatalogSettings.DefaultSkuConfiguration;

            // Create the ordered, immutable list, same for all configured subscriptions.
            var defaultLocations = new ReadOnlyCollection<AzureLocation>(defaultSkuConfiguration.Locations
                .Distinct()
                .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                .ToList());

            foreach (var item in skuCatalogSettings.CloudEnvironmentSkuSettings)
            {
                var skuName = item.Key;
                var cloudEnvironmentSettings = item.Value;

                if (Skus.ContainsKey(skuName))
                {
                    throw new InvalidOperationException($"The cloud environment sku '{skuName}' already exists. Is it listed more than once in appsettings.json?");
                }

                // Compute the sku configuration from its own settings or from the default settings.
                var skuConfiguration = cloudEnvironmentSettings.SkuConfiguration;
                if (skuConfiguration == null)
                {
                    skuConfiguration = defaultSkuConfiguration;
                }
                else
                {
                    /* TODO: move File Share configuration to SKU configuration.
                    skuConfiguration.FileShareTemplateBlobName = skuConfiguration.FileShareTemplateBlobName ?? defaultSkuConfiguration.FileShareTemplateBlobName;
                    skuConfiguration.FileShareTemplateContainerName = skuConfiguration.FileShareTemplateContainerName ?? defaultSkuConfiguration.FileShareTemplateContainerName;
                    */
                    if (skuConfiguration.PoolSize.GetValueOrDefault() == default)
                    {
                        skuConfiguration.PoolSize = defaultSkuConfiguration.PoolSize;
                    }

                    // This is a restrictive set. Defaults might be more. Only use defaults if none set.
                    if (!skuConfiguration.Locations.Any())
                    {
                        skuConfiguration.Locations.AddRange(defaultSkuConfiguration.Locations);
                    }

                    foreach (var vmItem in defaultSkuConfiguration.VMImages)
                    {
                        if (!skuConfiguration.VMImages.ContainsKey(vmItem.Key))
                        {
                            skuConfiguration.VMImages[vmItem.Key] = vmItem.Value;
                        }
                    }
                }

                // Create the ordered location list; filter to supported data-plane locations.
                var skuLocations = new ReadOnlyCollection<AzureLocation>(skuConfiguration.Locations
                    .Where(l => dataPlaneLocations.Contains(l))
                    .Distinct()
                    .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                    .ToList()
                    .AsReadOnly());

                // Get the VM image
                if (!skuConfiguration.VMImages.TryGetValue(cloudEnvironmentSettings.ComputeOS, out string vmImage))
                {
                    throw new InvalidOperationException($"A {nameof(skuConfiguration.VMImages)} entry is required for OS '{cloudEnvironmentSettings.ComputeOS}'.");
                }

                var cloudEnvironment = new CloudEnvironmentSku(
                    skuName,
                    cloudEnvironmentSettings.SkuDisplayName,
                    skuLocations,
                    cloudEnvironmentSettings.ComputeSkuFamily,
                    cloudEnvironmentSettings.ComputeSkuName,
                    cloudEnvironmentSettings.ComputeSkuSize,
                    cloudEnvironmentSettings.ComputeOS,
                    vmImage,
                    cloudEnvironmentSettings.StorageSkuName,
                    cloudEnvironmentSettings.StorageSizeInGB,
                    cloudEnvironmentSettings.StorageCloudEnvironmentUnits,
                    cloudEnvironmentSettings.ComputeCloudEnvironmentUnits,
                    skuConfiguration.PoolSize.GetValueOrDefault());

                Skus.Add(skuName, cloudEnvironment);
            }

            // The public, immutable dictionary of skus.
            CloudEnvironmentSkus = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(Skus);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, ICloudEnvironmentSku> CloudEnvironmentSkus { get; }

        private Dictionary<string, ICloudEnvironmentSku> Skus { get; } = new Dictionary<string, ICloudEnvironmentSku>();
    }
}
