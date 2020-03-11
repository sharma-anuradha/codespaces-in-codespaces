// <copyright file="ICloudEnvironmentSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents a Cloud Environment SKU.
    /// </summary>
    public interface ICloudEnvironmentSku
    {
        /// <summary>
        /// Gets the SKU name.
        /// </summary>
        string SkuName { get; }

        /// <summary>
        /// Gets the SKU display name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets a value indicating whether this SKU is enabled for creation.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets a value indicating whether this SKU represents hardware that we manage.
        /// </summary>
        bool IsExternalHardware { get; }

        /// <summary>
        /// Gets the list of locations supported for this subscription.
        /// </summary>
        IEnumerable<AzureLocation> SkuLocations { get; }

        /// <summary>
        /// Gets the Cloud Environment OS.
        /// </summary>
        ComputeOS ComputeOS { get; }

        /// <summary>
        /// Gets the SKU tier.
        /// </summary>
        SkuTier Tier { get; }

        /// <summary>
        /// Gets the Azure compute SKU family, e.g., "standardFSv2Family".
        /// </summary>
        string ComputeSkuFamily { get; }

        /// <summary>
        /// Gets the Azure compute SKU name, e.g., "Standard_F4s_v2".
        /// </summary>
        string ComputeSkuName { get; }

        /// <summary>
        /// Gets the Azure compute SKU size, e.g., "F4s_v2".
        /// </summary>
        string ComputeSkuSize { get; }

        /// <summary>
        /// Gets the number of cores.
        /// </summary>
        int ComputeSkuCores { get; }

        /// <summary>
        /// Gets the compute agent image family for this SKU.
        /// </summary>
        IBuildArtifactImageFamily VmAgentImage { get; }

        /// <summary>
        /// Gets the compute image family for this SKU.
        /// </summary>
        IVmImageFamily ComputeImage { get; }

        /// <summary>
        /// Gets the Azure storage SKU name: Premium_LRS, Premium_ZRS, Standard_GRS, Standard_LRS, Standard_RAGRS, or Standard_ZRS.
        /// </summary>
        string StorageSkuName { get; }

        /// <summary>
        /// Gets the storage image family for this SKU.
        /// </summary>
        IBuildArtifactImageFamily StorageImage { get; }

        /// <summary>
        /// Gets the requested file storage size in GB.
        /// </summary>
        int StorageSizeInGB { get; }

        /// <summary>
        /// Gets the number of VSO units per hour for storage.
        /// </summary>
        decimal StorageVsoUnitsPerHour { get; }

        /// <summary>
        /// Gets the number of VSO units per hour for compute.
        /// </summary>
        decimal ComputeVsoUnitsPerHour { get; }

        /// <summary>
        /// Gets the compute pool size that should be maintained.
        /// </summary>
        int ComputePoolLevel { get; }

        /// <summary>
        /// Gets the pool size that storage should be maintained.
        /// </summary>
        int StoragePoolLevel { get; }

        /// <summary>
        /// Gets the set of SKUs which environments using this SKU are allowed to migrate to.
        /// </summary>
        IEnumerable<string> SupportedSkuTransitions { get; }

        /// <summary>
        /// Gets the set feature flags for the Sku.
        /// </summary>
        IEnumerable<string> SupportedFeatureFlags { get; }
    }
}
