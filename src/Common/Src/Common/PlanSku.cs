// <copyright file="PlanSku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Represents a Plan SKU.
    /// </summary>
    public class PlanSku : IPlanSku
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlanSku"/> class.
        /// </summary>
        /// <param name="skuName">The sku name.</param>
        /// <param name="enabled">Indicates if the sku is enabled.</param>
        /// <param name="skuLocations">Supported sku locations.</param>
        /// <param name="keyVaultSkuName">Key vault sku name.</param>
        /// <param name="keyVaultPoolLevel">Key vault pool level.</param>
        /// <param name="supportedFeatureFlags">Supported feature flags.</param>
        public PlanSku(
            string skuName,
            bool enabled,
            IEnumerable<AzureLocation> skuLocations,
            string keyVaultSkuName,
            int keyVaultPoolLevel,
            IEnumerable<string> supportedFeatureFlags)
        {
            SkuName = skuName;
            Enabled = enabled;
            SkuLocations = skuLocations;
            KeyVaultSkuName = keyVaultSkuName;
            KeyVaultPoolLevel = keyVaultPoolLevel;
            SupportedFeatureFlags = supportedFeatureFlags;
        }

        /// <inheritdoc/>
        public string SkuName { get; set; }

        /// <inheritdoc/>
        public bool Enabled { get; set; }

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> SkuLocations { get; set; }

        /// <inheritdoc/>
        public string KeyVaultSkuName { get; set; }

        /// <inheritdoc/>
        public int KeyVaultPoolLevel { get; set; }

        /// <inheritdoc/>
        public IEnumerable<string> SupportedFeatureFlags { get; set; }
    }
}
