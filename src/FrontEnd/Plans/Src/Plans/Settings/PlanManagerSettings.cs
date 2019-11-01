// <copyright file="PlanManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using System.Threading.Tasks;

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
        public int GlobalPlanLimit { get; set; }

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
    }
}
