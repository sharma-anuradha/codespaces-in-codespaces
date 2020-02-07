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
        public Task<int> MaxPlansPerSubscriptionAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscriptionId, nameof(subscriptionId));
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetSubscriptionValueAsync("quota:max-plans-per-sub", subscriptionId, logger, DefaultMaxPlansPerSubscription);
        }

        /// <summary>
        /// Gets the system-wide (global) limit of plans in the VSO service.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> GetGlobalPlanLimitAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("quota:global-max-plans", logger, DefaultGlobalPlanLimit);
        }
    }
}
