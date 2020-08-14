// <copyright file="BillingSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts
{
    /// <summary>
    /// Contract representing billing meter definitions from appSettings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class BillingSettings
    {
        private ISystemConfiguration systemConfiguration;

        /// <summary>
        /// Gets or sets a value indicating whether transmission to push agent is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool EnableV2BillingManagementProducer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether transmission to push agent is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool EnableV2Transmission { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to archive old summaries and state changes.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool EnableV2Archiving { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to correct for missing environments.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool EnableV2CheckForMissingEnvironments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to correct for missing environments.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool EnableV2CheckForFinalStates { get; set; }

        /// <summary>
        /// Gets or sets the value of concurrent job consumers.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int ConcurrentJobConsumerCount { get; set; }

        /// <summary>
        /// Gets or sets the value of concurrent job consumers.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int ConcurrentJobProducerCount { get; set; }

        /// <summary>
        /// Initializes the class.
        /// </summary>
        /// <param name="systemConfiguration">System Configuration.</param>
        public void Init(ISystemConfiguration systemConfiguration)
        {
            this.systemConfiguration = systemConfiguration;
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<bool> V2BillingManagementProducerIsEnabledAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("featureflag:enable-billing-v2-billing-management-producer", logger, EnableV2BillingManagementProducer);
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<bool> V2TransmissionIsEnabledAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("featureflag:enable-billing-v2-transmission", logger, EnableV2Transmission);
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<int> V2ConcurrentJobProducerCountAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("setting:billing-v2-concurrent-job-producer-count", logger, ConcurrentJobProducerCount);
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<bool> V2EnableArchivingAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("featureflag:billing-v2-enable-archiving", logger, EnableV2Archiving);
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<bool> V2EnableV2CheckForMissingEnvironmentsAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("featureflag:billing-v2-enable-check-for-missing-environments", logger, EnableV2CheckForMissingEnvironments);
        }

        /// <summary>
        /// Get current the value of the feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public virtual Task<bool> V2EnableV2CheckForFinalStatesAsync(IDiagnosticsLogger logger)
        {
            Requires.NotNull(systemConfiguration, nameof(systemConfiguration));

            return systemConfiguration.GetValueAsync("featureflag:billing-v2-enable-check-for-final-states", logger, EnableV2CheckForFinalStates);
        }
    }
}
