// <copyright file="ICapacityManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// The back-end capacity manager. Handles _where_ resoures get allocated, based capacity and any other criteria.
    /// </summary>
    public interface ICapacityManager
    {
        /// <summary>
        /// Select a subscription for the given.
        /// </summary>
        /// <param name="sku">The cloud environment sku.</param>
        /// <param name="location">The requested azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A <see cref="IAzureSubscription"/> object.</returns>
        /// <exception cref="SkuNotAvailableException">The requested sku and location is not available in any subscription.</exception>
        Task<IAzureResourceLocation> SelectAzureResourceLocation(ICloudEnvironmentSku sku, AzureLocation location, IDiagnosticsLogger logger);

        /// <summary>
        /// Get all compute usage for the given subscription.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of azure usage, distinguished by quota.</returns>
        Task<IEnumerable<AzureResourceUsage>> GetComputeUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger);

        /// <summary>
        /// Get all network usage for the given subscription.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of azure usage, distinguished by quota.</returns>
        Task<IEnumerable<AzureResourceUsage>> GetNetworkUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger);

        /// <summary>
        /// Get all storage usage for the given subscription.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of azure usage, distinguished by quota.</returns>
        Task<IEnumerable<AzureResourceUsage>> GetStorageUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger);
    }
}
