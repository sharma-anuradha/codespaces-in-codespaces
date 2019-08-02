// <copyright file="CloudEnvironmentSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
{
    /// <inheritdoc/>
    public class CloudEnvironmentSku : ICloudEnvironmentSku
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentSku"/> class.
        /// </summary>
        /// <param name="skuName">The cloud environment sku name.</param>
        /// <param name="skuDisplayName">The cloud environment sku display name.</param>
        /// <param name="computeSkuFamily">The azure compute sku family.</param>
        /// <param name="computeSkuName">The azure compute sku name.</param>
        /// <param name="computeSkuSize">The azure compute sku size.</param>
        /// <param name="computeOS">The azure compute OS.</param>
        /// <param name="defaultVMImage">The default VM image for this SKU.</param>
        /// <param name="storageSkuName">The azure storage sku name.</param>
        /// <param name="storageSizeInGB">The azure storage size in GB.</param>
        /// <param name="storageCloudEnvironmentUnits">The cloud environment units for this sku when active.</param>
        /// <param name="computeCloudEnvironmentUnits">The cloud environment units for this sku when inactive.</param>
        public CloudEnvironmentSku(
            string skuName,
            string skuDisplayName,
            string computeSkuFamily,
            string computeSkuName,
            string computeSkuSize,
            ComputeOS computeOS,
            string defaultVMImage,
            string storageSkuName,
            int storageSizeInGB,
            decimal storageCloudEnvironmentUnits,
            decimal computeCloudEnvironmentUnits)
        {
            Requires.NotNullOrEmpty(skuName, nameof(skuName));
            Requires.NotNullOrEmpty(skuDisplayName, nameof(skuDisplayName));
            Requires.NotNullOrEmpty(computeSkuFamily, nameof(computeSkuFamily));
            Requires.NotNullOrEmpty(computeSkuName, nameof(computeSkuName));
            Requires.NotNullOrEmpty(computeSkuSize, nameof(computeSkuSize));
            Requires.Argument(Enum.IsDefined(typeof(ComputeOS), computeOS), nameof(computeOS), $"The value '{computeOS}' is not a value {nameof(ComputeOS)}.");
            Requires.NotNullOrEmpty(defaultVMImage, nameof(defaultVMImage));
            Requires.NotNullOrEmpty(storageSkuName, nameof(storageSkuName));
            Requires.Argument(storageSizeInGB > 0, nameof(storageSizeInGB), "The storage size must be greater than zero.");
            Requires.Argument(storageCloudEnvironmentUnits >= 0m, nameof(storageCloudEnvironmentUnits), "The cloud environment units must be greater than or equal to 0.");
            Requires.Argument(computeCloudEnvironmentUnits >= 0m, nameof(computeCloudEnvironmentUnits), "The cloud environment units must be greater than or equal to 0.");

            SkuName = skuName;
            SkuDisplayName = skuDisplayName;
            ComputeSkuFamily = computeSkuFamily;
            ComputeSkuName = computeSkuName;
            ComputeSkuSize = computeSkuSize;
            ComputeOS = computeOS;
            DefaultVMImage = defaultVMImage;
            StorageSkuName = storageSkuName;
            StorageSizeInGB = storageSizeInGB;
            StorageCloudEnvironmentUnits = storageCloudEnvironmentUnits;
            ComputeCloudEnvironmentUnits = computeCloudEnvironmentUnits;
        }

        /// <inheritdoc/>
        public string SkuName { get; }

        /// <inheritdoc/>
        public string SkuDisplayName { get; }

        /// <inheritdoc/>
        public string ComputeSkuFamily { get; }

        /// <inheritdoc/>
        public string ComputeSkuName { get; }

        /// <inheritdoc/>
        public string ComputeSkuSize { get; }

        /// <inheritdoc/>
        public ComputeOS ComputeOS { get; }

        /// <inheritdoc/>
        public string DefaultVMImage { get; }

        /// <inheritdoc/>
        public string StorageSkuName { get; }

        /// <inheritdoc/>
        public int StorageSizeInGB { get; }

        /// <inheritdoc/>
        public decimal StorageCloudEnvironmentUnits { get; }

        /// <inheritdoc/>
        public decimal ComputeCloudEnvironmentUnits { get; }
    }
}
