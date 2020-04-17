// <copyright file="SubscriptionManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <inheritdoc/>
    public class SubscriptionManager : ISubscriptionManager
    {
        private const string SystemConfigurationSubscriptionBannedKey = "subscriptionmanager:is-banned";
        private const int BannedDaysAgoDefault = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionManager"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="bannedSubscriptionRepository">The banned subscriptions repository.</param>
        /// <param name="systemConfiguration">The system configuration manager.</param>
        public SubscriptionManager(
            IOptions<SubscriptionManagerSettings> options,
            IBannedSubscriptionRepository bannedSubscriptionRepository,
            ISystemConfiguration systemConfiguration)
        {
            BannedSubscriptionRepository = Requires.NotNull(bannedSubscriptionRepository, nameof(bannedSubscriptionRepository));
            Settings = Requires.NotNull(options?.Value, nameof(options));
            SystemConfiguration = Requires.NotNull(systemConfiguration, nameof(systemConfiguration));
        }

        private IBannedSubscriptionRepository BannedSubscriptionRepository { get; }

        private SubscriptionManagerSettings Settings { get; }

        private ISystemConfiguration SystemConfiguration { get; }

        /// <inheritdoc/>
        public async Task<BannedSubscription> AddBannedSubscriptionAsync(string subscriptionId, BannedReason bannedReason, string byIdentity, IDiagnosticsLogger logger)
        {
            var bannedSubscription = new BannedSubscription
            {
                Id = Requires.NotNull(subscriptionId, nameof(subscriptionId)),
                BannedReason = bannedReason,
                BannedByIdentity = byIdentity,
            };

            bannedSubscription = await BannedSubscriptionRepository.CreateOrUpdateAsync(bannedSubscription, logger);
            return bannedSubscription;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<BannedSubscription>> GetRecentBannedSubscriptionsAsync(DateTime? bannedOnOrAfter, IDiagnosticsLogger logger)
        {
            var daysAgo = Settings.BannedDaysAgo.GetValueOrDefault(BannedDaysAgoDefault);
            var defaultBannedOnOrAfter = DateTime.UtcNow - TimeSpan.FromDays(daysAgo);
            var bannedSubscriptions = await BannedSubscriptionRepository.GetRecentBannedSubscriptionsAsync(bannedOnOrAfter.GetValueOrDefault(defaultBannedOnOrAfter), logger);
            return bannedSubscriptions.Items;
        }

        /// <inheritdoc/>
        public async Task<bool> IsBannedAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            // Default result
            var isBanned = false;

            // Logging housekeeping
            var duration = logger.StartDuration();
            logger.AddValue("SubscriptionId", subscriptionId);

            try
            {
                // We don't want two DB lookups for a success case. So just use the banned subscription repository for now.
                /*
                //isBanned = await SystemConfiguration.GetSubscriptionValueAsync(
                //    SystemConfigurationSubscriptionBannedKey,
                //    subscriptionId,
                //    logger.NewChildLogger(),
                //    false);
                //logger.AddReason("{nameof(SystemConfiguration)}:{SystemConfigurationSubscriptionBannedKey}");
                */

                if (!isBanned)
                {
                    var bannedSubscription = await BannedSubscriptionRepository.GetAsync(subscriptionId, logger.NewChildLogger());
                    logger.AddReason($"{bannedSubscription?.BannedReason}");
                    isBanned = bannedSubscription != null;
                }

                logger
                    .AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(IsBannedAsync)));
            }
            catch (Exception ex)
            {
                // Return not banned if we couldn't read a specific is-banned record.
                logger
                    .AddDuration(duration)
                    .LogException(GetType().FormatLogErrorMessage(nameof(IsBannedAsync)), ex);
            }

            return isBanned;
        }
    }
}
