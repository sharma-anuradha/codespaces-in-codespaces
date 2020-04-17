// <copyright file="ISubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// The front-end Subscription Manager.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Tests whether the given subscription id is banned.
        /// </summary>
        /// <param name="subscriptionId">The subscription id to be banned.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the subscription is banned.</returns>
        Task<bool> IsBannedAsync(
            string subscriptionId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Add a banned subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id to be banned.</param>
        /// <param name="bannedReason">The reason.</param>
        /// <param name="byIdentity">The email or user identity of who banned it.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The new <see cref="BannedSubscription"/> item.</returns>
        Task<BannedSubscription> AddBannedSubscriptionAsync(
            string subscriptionId,
            BannedReason bannedReason,
            string byIdentity,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get the set of recently banned subscriptions.
        /// </summary>
        /// <param name="bannedOnOrAfter">The date to filter on. If not specified, the implementation elects a default recent time.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The list of subscriptions.</returns>
        Task<IEnumerable<BannedSubscription>> GetRecentBannedSubscriptionsAsync(
            DateTime? bannedOnOrAfter,
            IDiagnosticsLogger logger);
    }
}
