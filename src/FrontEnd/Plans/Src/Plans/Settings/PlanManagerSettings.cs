// <copyright file="PlanManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class PlanManagerSettings
    {
        /// <summary>
        /// Gets or sets the SkuPlan Quota.
        /// </summary>
        public int DefaultMaxPlansPerSubscription { get; set; }

        /// <summary>
        /// Gets or sets the global Plan Quota.
        /// </summary>
        public int DefaultGlobalPlanLimit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vnet injection is enabled.
        /// </summary>
        public bool DefaultVnetInjectionEnabled { get; set; }

        /// <summary>
        /// Gets or sets the options for auto suspend delay minutes.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int[] DefaultAutoSuspendDelayMinutesOptions { get; set; }

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
        /// Get current max plans per subscription.
        /// </summary>
        /// <param name="subscriptionId">Target subscription id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<int> MaxPlansPerSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscriptionId, nameof(subscriptionId));
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var subscriptionLimit = await SystemConfiguration.GetSubscriptionValueAsync<int?>("quota:max-plans-per-sub", subscriptionId, logger, null);
            if (subscriptionLimit != null)
            {
                return subscriptionLimit.Value;
            }

            var globalLimit = await SystemConfiguration.GetValueAsync("quota:max-plans-per-sub", logger, DefaultMaxPlansPerSubscription);

            return globalLimit;
        }

        /// <summary>
        /// Gets the feature flag for enabling creating new plans as multi-user plans.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> MultiUserPlansEnabledAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:multi-user-plans", logger, false);
        }

        /// <summary>
        /// Gets or sets whether vnet injection is enabled.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> VnetInjectionEnabledAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:plan-vnet-injection-enabled", logger, DefaultVnetInjectionEnabled);
        }
    }
}
