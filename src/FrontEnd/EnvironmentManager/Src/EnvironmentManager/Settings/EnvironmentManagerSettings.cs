// <copyright file="EnvironmentManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class EnvironmentManagerSettings
    {
        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int DefaultMaxEnvironmentsPerPlan { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container that the Environment Manager
        /// can use for distributed leases.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string LeaseContainerName { get; set; }

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
        /// Get current max environments per plan.
        /// </summary>
        /// <param name="subscriptionId">Target subscription id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> MaxEnvironmentsPerPlanAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscriptionId, nameof(subscriptionId));
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetSubscriptionValueAsync("quota:max-environments-per-plan", subscriptionId, logger, DefaultMaxEnvironmentsPerPlan);
        }
    }
}
