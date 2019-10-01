// <copyright file="ICapacityManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// The back-end capacity manager. Handles _where_ resoures get allocated, based on capacity and any other criteria.
    /// </summary>
    public interface ICapacityManager
    {
        /// <summary>
        /// Select a subscription for the given.
        /// </summary>
        /// <param name="criteria">The list of required criteria. All must be satisfied.</param>
        /// <param name="location">The requested azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A <see cref="IAzureSubscription"/> object.</returns>
        /// <exception cref="LocationNotAvailableException">The requested location is not available in any subscription.</exception>
        /// <exception cref="CapacityNotAvailableException">The requested capacity is not available in any subscription.</exception>
        Task<IAzureResourceLocation> SelectAzureResourceLocation(IEnumerable<AzureResourceCriterion> criteria, AzureLocation location, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets list of all azure resource groups.
        /// </summary>
        /// <param name="azureClientFactory">Azure client factory.</param>
        /// <returns>Complete list of resource groups.</returns>
        Task<IEnumerable<IAzureResourceGroup>> SelectAllAzureResourceGroups(IAzureClientFactory azureClientFactory);
    }
}
