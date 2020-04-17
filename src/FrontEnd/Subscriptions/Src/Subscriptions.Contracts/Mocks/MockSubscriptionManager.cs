// <copyright file="MockSubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

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
        public Task<BannedSubscription> AddBannedSubscriptionAsync(string subscriptionId, BannedReason bannedReason, string byIdentity, IDiagnosticsLogger logger)
        {
            var bannedSubscription = new BannedSubscription
            {
                Id = Requires.NotNull(subscriptionId, nameof(subscriptionId)),
                BannedReason = bannedReason,
                BannedByIdentity = byIdentity,
            };

            return Task.FromResult(bannedSubscription);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BannedSubscription>> GetRecentBannedSubscriptionsAsync(DateTime? bannedOnOrAfter, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            return Enumerable.Empty<BannedSubscription>();
        }

        /// <inheritdoc/>
        public Task<bool> IsBannedAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            return Task.FromResult(false);
        }
    }
}
