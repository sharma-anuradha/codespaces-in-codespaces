// <copyright file="CloudEnvironmentSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class CloudEnvironmentSku : ICloudEnvironmentSku
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentSku"/> class.
        /// </summary>
        /// <param name="skuName">The cloud environment sku name.</param>
        /// <param name="skuDisplayName">The cloud environment sku display name.</param>
        /// <param name="skuLocations">The locations in which this sku is available.</param>
        /// <param name="computeSkuFamily">The azure compute sku family.</param>
        /// <param name="computeSkuName">The azure compute sku name.</param>
        /// <param name="computeSkuSize">The azure compute sku size.</param>
        /// <param name="computeOS">The azure compute OS.</param>
        /// <param name="vmAgentImageFamily">The default VSO Agent image for this SKU.</param>
        /// <param name="computeImageFamily">The default VM image for this SKU.</param>
        /// <param name="storageSkuName">The azure storage sku name.</param>
        /// <param name="storageImageFamily">The storage image family.</param>
        /// <param name="storageSizeInGB">The azure storage size in GB.</param>
        /// <param name="storageCloudEnvironmentUnits">The cloud environment units for this sku when active.</param>
        /// <param name="computeCloudEnvironmentUnits">The cloud environment units for this sku when inactive.</param>
        /// <param name="computePoolLevel">The size of the compute pool that should be maintained.</param>
        /// <param name="storagePoolLevel">The size of the storage pool that should be maintained.</param>
        public CloudEnvironmentSku(
            string skuName,
            string skuDisplayName,
            IReadOnlyCollection<AzureLocation> skuLocations,
            string computeSkuFamily,
            string computeSkuName,
            string computeSkuSize,
            ComputeOS computeOS,
            IBuildArtifactImageFamily vmAgentImageFamily,
            IVmImageFamily computeImageFamily,
            string storageSkuName,
            IBuildArtifactImageFamily storageImageFamily,
            int storageSizeInGB,
            decimal storageCloudEnvironmentUnits,
            decimal computeCloudEnvironmentUnits,
            int computePoolLevel,
            int storagePoolLevel)
        {
            Requires.NotNullOrEmpty(skuName, nameof(skuName));
            Requires.NotNullOrEmpty(skuDisplayName, nameof(skuDisplayName));
            Requires.NotNullOrEmpty(computeSkuFamily, nameof(computeSkuFamily));
            Requires.NullOrNotNullElements(skuLocations, nameof(skuLocations));
            Requires.NotNullOrEmpty(computeSkuName, nameof(computeSkuName));
            Requires.NotNullOrEmpty(computeSkuSize, nameof(computeSkuSize));
            Requires.Argument(Enum.IsDefined(typeof(ComputeOS), computeOS), nameof(computeOS), $"The value '{computeOS}' is not a value {nameof(ComputeOS)}.");
            Requires.NotNull(vmAgentImageFamily, nameof(vmAgentImageFamily));
            Requires.NotNull(computeImageFamily, nameof(computeImageFamily));
            Requires.NotNullOrEmpty(storageSkuName, nameof(storageSkuName));
            Requires.NotNull(storageImageFamily, nameof(storageImageFamily));
            Requires.Argument(storageSizeInGB > 0, nameof(storageSizeInGB), "The storage size must be greater than zero.");
            Requires.Argument(storageCloudEnvironmentUnits >= 0m, nameof(storageCloudEnvironmentUnits), "The cloud environment units must be greater than or equal to 0.");
            Requires.Argument(computeCloudEnvironmentUnits >= 0m, nameof(computeCloudEnvironmentUnits), "The cloud environment units must be greater than or equal to 0.");
            Requires.Argument(computePoolLevel > 0, nameof(computePoolLevel), "The compute pool level must be greater than zero.");
            Requires.Argument(storagePoolLevel > 0, nameof(storagePoolLevel), "The storage pool level must be greater than zero.");

            SkuName = skuName;
            SkuDisplayName = skuDisplayName;
            SkuLocations = skuLocations;
            ComputeSkuFamily = computeSkuFamily;
            ComputeSkuName = computeSkuName;
            ComputeSkuSize = computeSkuSize;
            ComputeOS = computeOS;
            VmAgentImage = vmAgentImageFamily;
            ComputeImage = computeImageFamily;
            StorageSkuName = storageSkuName;
            StorageImage = storageImageFamily;
            StorageSizeInGB = storageSizeInGB;
            StorageCloudEnvironmentUnits = storageCloudEnvironmentUnits;
            ComputeCloudEnvironmentUnits = computeCloudEnvironmentUnits;
            ComputePoolLevel = computePoolLevel;
            StoragePoolLevel = storagePoolLevel;
        }

        /// <inheritdoc/>
        public string SkuName { get; }

        /// <inheritdoc/>
        public string SkuDisplayName { get; }

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> SkuLocations { get; }

        /// <inheritdoc/>
        public string ComputeSkuFamily { get; }

        /// <inheritdoc/>
        public string ComputeSkuName { get; }

        /// <inheritdoc/>
        public string ComputeSkuSize { get; }

        /// <inheritdoc/>
        public ComputeOS ComputeOS { get; }

        /// <inheritdoc/>
        public IBuildArtifactImageFamily VmAgentImage { get; }

        /// <inheritdoc/>
        public IVmImageFamily ComputeImage { get; }

        /// <inheritdoc/>
        public string StorageSkuName { get; }

        /// <inheritdoc/>
        public IBuildArtifactImageFamily StorageImage { get; }

        /// <inheritdoc/>
        public int StorageSizeInGB { get; }

        /// <inheritdoc/>
        public decimal StorageCloudEnvironmentUnits { get; }

        /// <inheritdoc/>
        public decimal ComputeCloudEnvironmentUnits { get; }

        /// <inheritdoc/>
        public int ComputePoolLevel { get; }

        /// <inheritdoc/>
        public int StoragePoolLevel { get; }
    }
}
