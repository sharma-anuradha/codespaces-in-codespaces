// <copyright file="SkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.Azure.Management.Compute.Fluent.Models;
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
        /// <param name="controlPlaneAzureResourceAccessor">The control-plane resource accessor.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            IOptions<SkuCatalogOptions> skuCatalogOptions,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
            : this(skuCatalogOptions.Value.Settings, controlPlaneInfo, controlPlaneAzureResourceAccessor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCatalog"/> class.
        /// </summary>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        /// <param name="controlPlaneAzureResourceAccessor">The control-plane resource accessor.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            SkuCatalogSettings skuCatalogSettings,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor)
        {
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));

            // Get the mapping from family to VM agent.
            BuildArtifactImageFamilies = new ReadOnlyDictionary<string, IBuildArtifactImageFamily>(
                skuCatalogSettings.VmAgentImageFamilies.ToDictionary(e => e.Key, e => new BuildArtifactImageFamily(e.Key, e.Value.ImageName) as IBuildArtifactImageFamily));

            // Get the supported azure locations
            var dataPlaneLocations = new HashSet<AzureLocation>(controlPlaneInfo.Stamp.DataPlaneLocations);

            // Get the subscription id
            var subscriptionId = controlPlaneAzureResourceAccessor.GetCurrentSubscriptionIdAsync().Result;
            var vmImageResourceGroup = controlPlaneInfo.EnvironmentResourceGroupName;

            foreach (var item in skuCatalogSettings.CloudEnvironmentSkuSettings)
            {
                var skuName = item.Key;
                var cloudEnvironmentSettings = item.Value;

                if (Skus.ContainsKey(skuName))
                {
                    throw new InvalidOperationException($"The cloud environment sku '{skuName}' already exists. Is it listed more than once in appsettings.json?");
                }

                var computeOS = cloudEnvironmentSettings.ComputeOS;
                var defaultSkuConfiguration = skuCatalogSettings.DefaultSkuConfiguration[computeOS];
                var tier = cloudEnvironmentSettings.Tier;
                var tierSettings = skuCatalogSettings.SkuTierSettings[tier];

                // Compute the sku configuration from its own settings or from the default settings.
                var skuConfiguration = cloudEnvironmentSettings.SkuConfiguration;
                if (skuConfiguration == null)
                {
                    skuConfiguration = defaultSkuConfiguration;
                }
                else
                {
                    // Compute pool size setup
                    if (skuConfiguration.ComputePoolSize.GetValueOrDefault() == default)
                    {
                        skuConfiguration.ComputePoolSize = defaultSkuConfiguration.ComputePoolSize;
                    }

                    // Storage pool size setup
                    if (skuConfiguration.StoragePoolSize.GetValueOrDefault() == default)
                    {
                        skuConfiguration.StoragePoolSize = defaultSkuConfiguration.StoragePoolSize;
                    }

                    // The compute image family
                    if (string.IsNullOrEmpty(skuConfiguration.ComputeImageFamily))
                    {
                        skuConfiguration.ComputeImageFamily = defaultSkuConfiguration.ComputeImageFamily;
                    }

                    // The storage image family
                    if (string.IsNullOrEmpty(skuConfiguration.StorageImageFamily))
                    {
                        skuConfiguration.StorageImageFamily = defaultSkuConfiguration.StorageImageFamily;
                    }

                    // The storage image family
                    if (string.IsNullOrEmpty(skuConfiguration.VmAgentImageFamily))
                    {
                        skuConfiguration.VmAgentImageFamily = defaultSkuConfiguration.VmAgentImageFamily;
                    }

                    // This is a restrictive set. Defaults might be more. Only use defaults if none set.
                    if (!skuConfiguration.Locations.Any())
                    {
                        skuConfiguration.Locations.AddRange(defaultSkuConfiguration.Locations);
                    }
                }

                // Create the ordered location list; filter to supported data-plane locations.
                var skuLocations = new ReadOnlyCollection<AzureLocation>(skuConfiguration.Locations
                    .Where(l => dataPlaneLocations.Contains(l))
                    .Distinct()
                    .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                    .ToList()
                    .AsReadOnly());

                // Get the VM and storage image familes.
                var computeImageFamily = NewVmImageFamily(
                    skuConfiguration.ComputeImageFamily,
                    skuCatalogSettings.ComputeImageFamilies,
                    subscriptionId,
                    vmImageResourceGroup);

                var vmAgentImageFamily = CreateBuildArtifactImageFamily(
                    skuConfiguration.VmAgentImageFamily,
                    skuCatalogSettings.VmAgentImageFamilies);

                var storageImageFamily = CreateBuildArtifactImageFamily(
                    skuConfiguration.StorageImageFamily,
                    skuCatalogSettings.StorageImageFamilies);

                var cloudEnvironment = new CloudEnvironmentSku(
                    skuName,
                    tier,
                    cloudEnvironmentSettings.DisplayName,
                    cloudEnvironmentSettings.Enabled,
                    skuLocations,
                    tierSettings.ComputeSkuFamily,
                    tierSettings.ComputeSkuName,
                    tierSettings.ComputeSkuSize,
                    tierSettings.ComputeSkuCores,
                    computeOS,
                    vmAgentImageFamily,
                    computeImageFamily,
                    tierSettings.StorageSkuName,
                    storageImageFamily,
                    tierSettings.StorageSizeInGB,
                    cloudEnvironmentSettings.StorageVsoUnitsPerHour,
                    cloudEnvironmentSettings.ComputeVsoUnitsPerHour,
                    cloudEnvironmentSettings.Enabled ? skuConfiguration.ComputePoolSize.GetValueOrDefault() : 0,
                    cloudEnvironmentSettings.Enabled ? skuConfiguration.StoragePoolSize.GetValueOrDefault() : 0);

                Skus.Add(skuName, cloudEnvironment);
            }

            // Add a SKU for static environments.
            var staticEnvironmentSku = new StaticEnvironmentSku();
            Skus.Add(staticEnvironmentSku.SkuName, staticEnvironmentSku);

            // The public, immutable dictionary of skus.
            CloudEnvironmentSkus = new ReadOnlyDictionary<string, ICloudEnvironmentSku>(Skus);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, ICloudEnvironmentSku> CloudEnvironmentSkus { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, IBuildArtifactImageFamily> BuildArtifactImageFamilies { get; }

        private Dictionary<string, ICloudEnvironmentSku> Skus { get; } = new Dictionary<string, ICloudEnvironmentSku>();

        private static IVmImageFamily NewVmImageFamily(
            string imageFamilyName,
            IDictionary<string, VmImageFamilySettings> imageFamilies,
            string vmImageSubscriptionId,
            string vmImageResourceGroup)
        {
            if (!imageFamilies.TryGetValue(imageFamilyName, out var imageFamilySettings))
            {
                throw new NotSupportedException($"The image family name '{imageFamilyName}' is not configured.");
            }

            var imageFamily = new VmImageFamily(
                imageFamilyName,
                imageFamilySettings.ImageKind,
                imageFamilySettings.ImageName,
                imageFamilySettings.ImageVersion,
                vmImageSubscriptionId,
                vmImageResourceGroup);

            return imageFamily;
        }

        private static IBuildArtifactImageFamily CreateBuildArtifactImageFamily(
            string imageFamilyName,
            IDictionary<string, ImageFamilySettings> imageFamilies)
        {
            if (!imageFamilies.TryGetValue(imageFamilyName, out var imageFamilySettings))
            {
                throw new NotSupportedException($"The image family name '{imageFamilyName}' is not configured.");
            }

            var imageFamily = new BuildArtifactImageFamily(
                imageFamilyName,
                imageFamilySettings.ImageName);

            return imageFamily;
        }
    }
}
