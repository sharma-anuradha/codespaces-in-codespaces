// <copyright file="IPlanSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents a Plan SKU.
    /// </summary>
    public interface IPlanSku
    {
        /// <summary>
        /// Gets the SKU name.
        /// </summary>
        string SkuName { get; }

        /// <summary>
        /// Gets a value indicating whether this SKU is enabled for creation.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets the list of locations supported for this subscription.
        /// </summary>
        IEnumerable<AzureLocation> SkuLocations { get; }

        /// <summary>
        /// Gets the Azure key vault SKU name, e.g., "Standard", "Premium".
        /// </summary>
        string KeyVaultSkuName { get; }

        /// <summary>
        /// Gets the key vault pool size that should be maintained.
        /// </summary>
        int KeyVaultPoolLevel { get; }

        /// <summary>
        /// Gets the set feature flags for the Sku.
        /// </summary>
        IEnumerable<string> SupportedFeatureFlags { get; }
    }
}
