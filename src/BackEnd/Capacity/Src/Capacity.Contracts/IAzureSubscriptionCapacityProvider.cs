// <copyright file="IAzureSubscriptionCapacityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Gets the set of azure resource usages configured for the given azure subscription.
    /// See <see cref="IAzureSubscription.ComputeQuotas"/>, <see cref="IAzureSubscription.NetworkQuotas"/>, and <see cref="IAzureSubscription.StorageQuotas"/>.
    /// </summary>
    public interface IAzureSubscriptionCapacityProvider
    {
        /// <summary>
        /// Loads the the resource usage for the given subscription, location, and resource type, from the capacity repository.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="serviceType">The azure service type.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The resource usage.</returns>
        Task<IEnumerable<AzureResourceUsage>> LoadAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates the resource useage for the given subscription, location, and resource type, into the capacity repository.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="serviceType">The azure service type.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task UpdateAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger);


        /// <summary>
        /// Gets the the live resource usage for the given subscription, location, and resource type, from ARM.
        /// </summary>
        /// <param name="subscription">The azure subscription.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="serviceType">The azure service type.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The resource usage.</returns>
        Task<IEnumerable<AzureResourceUsage>> GetAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger);
    }
}
