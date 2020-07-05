// <copyright file="MockSubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Mocks
{
    /// <inheritdoc/>
    public class MockSubscriptionManager : ISubscriptionManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockSubscriptionManager"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="bannedSubscriptionRepository">The banned subscriptions repository.</param>
        public MockSubscriptionManager()
        {
        }

        /// <inheritdoc/>
        public Task<Subscription> AddBannedSubscriptionAsync(string subscriptionId, BannedReason bannedReason, string byIdentity, IDiagnosticsLogger logger)
        {
            var bannedSubscription = new Subscription
            {
                Id = Requires.NotNull(subscriptionId, nameof(subscriptionId)),
                BannedReason = bannedReason,
                BannedByIdentity = byIdentity,
            };

            return Task.FromResult(bannedSubscription);
        }

        /// <inheritdoc/>
        public Task<Subscription> GetSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger, string resourceProvider = null)
        {
            return Task.FromResult(new Subscription());
        }

        /// <inheritdoc/>
        public Task<Subscription> UpdateSubscriptionStateAsync(Subscription subscriptionId, SubscriptionStateEnum state, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> CanSubscriptionCreatePlansAndEnvironmentsAsync(Subscription subscriptionId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        Task<RPRegisteredSubscriptionsRequest> ISubscriptionManager.GetSubscriptionDetailsFromExternalSourceAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Subscription> UpdateSubscriptionQuotaAsync(Subscription subscription, string quotaId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Subscription>> GetRecentBannedSubscriptionsAsync(IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<Subscription> UpdatedCompletedBannedSubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
