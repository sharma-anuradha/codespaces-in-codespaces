// <copyright file="IBannedSubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Represents a repository of banned subscriptions.
    /// </summary>
    public interface IBannedSubscriptionRepository : ICosmosContainer<BannedSubscription>
    {
        /// <summary>
        /// Gets the set of subscriptions banned on or after the given date.
        /// </summary>
        /// <param name="bannedOnOrAfter">The filter date.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of banned subscriptions.</returns>
        Task<QueryResults<BannedSubscription>> GetRecentBannedSubscriptionsAsync(DateTime bannedOnOrAfter, IDiagnosticsLogger logger);
    }
}
