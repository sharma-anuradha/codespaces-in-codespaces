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
        string SkuDisplayName { get; }

        /// <summary>
        /// Gets the list of locations supported for this subscription.
        /// </summary>
        IEnumerable<AzureLocation> SkuLocations { get; }

        /// <summary>
        /// Gets the Cloud Environment OS.
        /// </summary>
        ComputeOS ComputeOS { get; }

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
        /// Gets the default VM image for this SKU.
        /// </summary>
        string DefaultVMImage { get; }

        /// <summary>
        /// Gets the Azure storage SKU name: Premium_LRS, Premium_ZRS, Standard_GRS, Standard_LRS, Standard_RAGRS, or Standard_ZRS.
        /// </summary>
        string StorageSkuName { get; }

        /// <summary>
        /// Gets the requested file storage size in GB.
        /// </summary>
        int StorageSizeInGB { get; }

        /// <summary>
        /// Gets the number of Cloud Environment Units that will be billed for this SKU when storage is active.
        /// </summary>
        decimal StorageCloudEnvironmentUnits { get; }

        /// <summary>
        /// Gets the number of Cloud Environment Units that will be billed for this SKU when compute is active.
        /// </summary>
        decimal ComputeCloudEnvironmentUnits { get; }

        /// <summary>
        /// Gets the pool size that should be maintained.
        /// </summary>
        int PoolLevel { get; }
    }
}
