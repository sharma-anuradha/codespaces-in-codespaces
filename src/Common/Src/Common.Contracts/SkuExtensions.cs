// <copyright file="SkuExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// <see cref="ICloudEnvironmentSku"/> extensions.
    /// </summary>
    public static class SkuExtensions
    {
        /// <summary>
        /// Gets the active VSO units per hour.
        /// </summary>
        /// <param name="sku">The sku.</param>
        /// <returns>The VSO units per hour when the SKU is active.</returns>
        public static decimal GetActiveVsoUnitsPerHour(this ICloudEnvironmentSku sku)
        {
            return sku.ComputeVsoUnitsPerHour + sku.StorageVsoUnitsPerHour;
        }

        /// <summary>
        /// Gets the inactive VSO units per hour.
        /// </summary>
        /// <param name="sku">The sku.</param>
        /// <returns>The VSO units per hour when the SKU is inactive.</returns>
        public static decimal GetInactiveVsoUnitsPerHour(this ICloudEnvironmentSku sku)
        {
            return sku.StorageVsoUnitsPerHour;
        }

        /// <summary>
        /// Gets enabled, internally-managed hardware. This includes Cloud Environments and excludes Static Environments.
        /// </summary>
        /// <param name="catalog">SKU Catalog from which to select.</param>
        /// <returns>Enabled, internall-managed hardware.</returns>
        public static IReadOnlyDictionary<string, ICloudEnvironmentSku> EnabledInternalHardware(this ISkuCatalog catalog)
        {
            return catalog.CloudEnvironmentSkus
                .Where(kv => kv.Value.Enabled && !kv.Value.IsExternalHardware)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
