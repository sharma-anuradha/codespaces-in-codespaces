// <copyright file="SubscriptionManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class SubscriptionManagerSettings
    {
        /// <summary>
        /// Gets or sets how long ago to process recently banned subscriptions.
        /// </summary>
        public int BannedDaysAgo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Subscription State Check feature is enabled.
        /// </summary>
        public bool IsSubscriptionStateCheckEnabled { get; set; }

        private ISystemConfiguration SystemConfiguration { get; set; }

        /// <summary>
        /// Initializes class.
        /// </summary>
        /// <param name="systemConfiguration">Target system configuration.</param>
        public void Init(ISystemConfiguration systemConfiguration)
        {
            SystemConfiguration = systemConfiguration;
        }

        /// <summary>
        /// Get feature flag for Subscription State check.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<bool> GetSubscriptionStateCheckFeatureFlagAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var isFeatureEnabled = await SystemConfiguration.GetValueAsync("featureflag:enable-sub-state-check", logger, IsSubscriptionStateCheckEnabled);

            return isFeatureEnabled;
        }
    }
}
