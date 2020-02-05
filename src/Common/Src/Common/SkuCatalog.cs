// <copyright file="SkuCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
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
        /// <param name="currentImageInfoProvider">The current image info provider.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            IOptions<SkuCatalogOptions> skuCatalogOptions,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            ICurrentImageInfoProvider currentImageInfoProvider)
            : this(
                  skuCatalogOptions.Value.Settings,
                  controlPlaneInfo,
                  controlPlaneAzureResourceAccessor,
                  currentImageInfoProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCatalog"/> class.
        /// </summary>
        /// <param name="skuCatalogSettings">The sku catalog settings.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="controlPlaneAzureResourceAccessor">The control-plane resource accessor.</param>
        /// <param name="currentImageInfoProvider">The current image info provider.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public SkuCatalog(
            SkuCatalogSettings skuCatalogSettings,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            ICurrentImageInfoProvider currentImageInfoProvider)
        {
            Requires.NotNull(skuCatalogSettings, nameof(skuCatalogSettings));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));

            // Get the mapping from family to VM agent.
            BuildArtifactImageFamilies = new ReadOnlyDictionary<string, IBuildArtifactImageFamily>(
                skuCatalogSettings.VmAgentImageFamilies.ToDictionary(
                    e => e.Key,
                    e => new BuildArtifactImageFamily(
                        ImageFamilyType.VmAgent,
                        e.Key,
                        e.Value.ImageName,
                        currentImageInfoProvider) as IBuildArtifactImageFamily));

            // Get the supported azure locations
            var dataPlaneLocations = new HashSet<AzureLocation>(controlPlaneInfo.Stamp.DataPlaneLocations);

            // Get the subscription id
            var subscriptionId = controlPlaneAzureResourceAccessor.GetCurrentSubscriptionIdAsync().Result;

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

                var supportedSkuTransitions = cloudEnvironmentSettings.SupportedSkuTransitions == null
                    ? Array.Empty<string>()
                    : cloudEnvironmentSettings.SupportedSkuTransitions.Distinct().ToArray();

                // Get the VM and storage image familes.
                var computeImageFamily = NewVmImageFamily(
                    controlPlaneInfo.Stamp,
                    skuConfiguration.ComputeImageFamily,
                    skuCatalogSettings.ComputeImageFamilies,
                    subscriptionId,
                    currentImageInfoProvider);

                var vmAgentImageFamily = CreateBuildArtifactImageFamily(
                    ImageFamilyType.VmAgent,
                    skuConfiguration.VmAgentImageFamily,
                    skuCatalogSettings.VmAgentImageFamilies,
                    currentImageInfoProvider);

                var storageImageFamily = CreateBuildArtifactImageFamily(
                    ImageFamilyType.Storage,
                    skuConfiguration.StorageImageFamily,
                    skuCatalogSettings.StorageImageFamilies,
                    currentImageInfoProvider);

                var enabled = cloudEnvironmentSettings.Enabled && tierSettings.Enabled && defaultSkuConfiguration.Enabled;

                var cloudEnvironment = new CloudEnvironmentSku(
                    skuName,
                    tier,
                    cloudEnvironmentSettings.DisplayName,
                    enabled,
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
                    enabled ? skuConfiguration.ComputePoolSize.GetValueOrDefault() : 0,
                    enabled ? skuConfiguration.StoragePoolSize.GetValueOrDefault() : 0,
                    supportedSkuTransitions);

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
            IControlPlaneStampInfo stampInfo,
            string imageFamilyName,
            IDictionary<string, VmImageFamilySettings> imageFamilies,
            string vmImageSubscriptionId,
            ICurrentImageInfoProvider currentImageInfoProvider)
        {
            if (!imageFamilies.TryGetValue(imageFamilyName, out var imageFamilySettings))
            {
                throw new NotSupportedException($"The image family name '{imageFamilyName}' is not configured.");
            }

            var imageFamily = new VmImageFamily(
                stampInfo,
                imageFamilyName,
                imageFamilySettings.ImageKind,
                imageFamilySettings.ImageName,
                imageFamilySettings.ImageVersion,
                vmImageSubscriptionId,
                currentImageInfoProvider);

            return imageFamily;
        }

        private static IBuildArtifactImageFamily CreateBuildArtifactImageFamily(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            IDictionary<string, ImageFamilySettings> imageFamilies,
            ICurrentImageInfoProvider currentImageInfoProvider)
        {
            if (!imageFamilies.TryGetValue(imageFamilyName, out var imageFamilySettings))
            {
                throw new NotSupportedException($"The image family name '{imageFamilyName}' is not configured.");
            }

            var imageFamily = new BuildArtifactImageFamily(
                imageFamilyType,
                imageFamilyName,
                imageFamilySettings.ImageName,
                currentImageInfoProvider);

            return imageFamily;
        }
    }
}
