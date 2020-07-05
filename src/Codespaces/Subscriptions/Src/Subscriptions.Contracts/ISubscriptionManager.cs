// <copyright file="ISubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// The front-end Subscription Manager.
    /// </summary>
    public interface ISubscriptionManager
    {
        /// <summary>
        /// Add a banned subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id to be banned.</param>
        /// <param name="bannedReason">The reason.</param>
        /// <param name="byIdentity">The email or user identity of who banned it.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The new <see cref="Subscription"/> item.</returns>
        Task<Subscription> AddBannedSubscriptionAsync(
            string subscriptionId,
            BannedReason bannedReason,
            string byIdentity,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get the set of recently banned subscriptions.
        /// </summary>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The list of subscriptions.</returns>
        Task<IEnumerable<Subscription>> GetRecentBannedSubscriptionsAsync(
            IDiagnosticsLogger logger);

        /// <summary>
        /// Updates the subscription's banned state so that it will no longer be processed.
        /// </summary>
        /// <param name="subscription">The subscription to update.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>a task that represents the update.</returns>
        Task<Subscription> UpdatedCompletedBannedSubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets a subscription.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="logger">the logger.</param>
        /// <param name="resourceProvider">the name of the resorce provider.</param>
        /// <returns>the subscription record.</returns>
        Task<Subscription> GetSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger, string resourceProvider = null);

        /// <summary>
        /// Updates the subscription state.
        /// </summary>
        /// <param name="subscriptionId">The desired subscription.</param>
        /// <param name="state">the new state.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>A task that shows completion of the update.</returns>
        Task<Subscription> UpdateSubscriptionStateAsync(Subscription subscriptionId, SubscriptionStateEnum state, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks is the subscription is allowed to create plans and environments.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>True if the subscription is 'Registered', otherwise false.</returns>
        Task<bool> CanSubscriptionCreatePlansAndEnvironmentsAsync(Subscription subscriptionId, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all the subscription details from RPSaaS for use in our system.
        /// </summary>
        /// <param name="subscription">the subscription ID we're looking for information on.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>Details about the subscription.</returns>
        Task<RPRegisteredSubscriptionsRequest> GetSubscriptionDetailsFromExternalSourceAsync(Subscription subscription, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates the subscription quotaId.
        /// </summary>
        /// <param name="subscription">The subscription that's being updated.</param>
        /// <param name="quotaId">the new quotaId.</param>
        /// <param name="logger">the logger.</param>
        /// <returns>A task that indicates completion.</returns>
        Task<Subscription> UpdateSubscriptionQuotaAsync(Subscription subscription, string quotaId, IDiagnosticsLogger logger);
    }
}
