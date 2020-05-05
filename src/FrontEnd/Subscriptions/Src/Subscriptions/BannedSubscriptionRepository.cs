// <copyright file="BannedSubscriptionRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Cosmos;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// The banned subscriptions repository.
    /// </summary>
    [ContainerId(CollectionId)]
    public class BannedSubscriptionRepository : CosmosContainer<BannedSubscription>, IBannedSubscriptionRepository
    {
        /// <summary>
        /// The banned subscriptions collection id.
        /// </summary>
        public const string CollectionId = "banned_subscriptions";

        /// <summary>
        /// Initializes a new instance of the <see cref="BannedSubscriptionRepository"/> class.
        /// </summary>
        /// <param name="options">The container options.</param>
        /// <param name="clientProvider">The doc db client provider.</param>
        /// <param name="healthProvider">The health provider.</param>
        /// <param name="loggerFactory">The diagnostics logging factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public BannedSubscriptionRepository(
            IOptionsMonitor<CosmosContainerOptions> options,
            ICosmosClientProvider clientProvider,
            IHealthProvider healthProvider,
            IDiagnosticsLoggerFactory loggerFactory,
            LogValueSet defaultLogValues)
            : base(
                options,
                clientProvider,
                healthProvider,
                loggerFactory,
                defaultLogValues)
        {
        }

        /// <summary>
        /// Configures the standard options for this repository.
        /// </summary>
        /// <param name="options">The options instance.</param>
        public static void ConfigureOptions(CosmosContainerOptions options)
        {
            Requires.NotNull(options, nameof(options));
            options.SetPartitioningStrategy(PartitioningStrategy.IdOnly);
        }

        /// <inheritdoc/>
        public async Task<QueryResults<BannedSubscription>> GetRecentBannedSubscriptionsAsync(DateTime bannedOnOrAfter, IDiagnosticsLogger logger)
        {
            var queriable = await GetQueriableAsync<BannedSubscription>(logger);
            var bannedSubscriptionsQuery = queriable
                .Where(item => item.DateBanned >= bannedOnOrAfter);
            return await QueryAsync("GetRecentBannedSubscriptions", bannedSubscriptionsQuery, logger);
        }
    }
}
