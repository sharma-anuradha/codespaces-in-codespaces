// <copyright file="SkuExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    }
}
