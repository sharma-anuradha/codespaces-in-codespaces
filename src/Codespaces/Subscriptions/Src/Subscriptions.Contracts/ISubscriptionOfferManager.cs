// <copyright file="ISubscriptionOfferManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions.Contracts
{
    /// <summary>
    /// Interface for the Quota Manager.
    /// </summary>
    public interface ISubscriptionOfferManager
    {
        /// <summary>
        /// Gets the quota value for a given offer.
        /// </summary>
        /// <param name="offerID">The offerID.</param>
        /// <param name="family">the sku family being sought.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>The maximum quota value for the subscription.</returns>
        Task<int> GetComputeQuotaForOfferAsync(string offerID, string family, IDiagnosticsLogger logger);
    }
}