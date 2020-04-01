// <copyright file="EnvironmentManagerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class EnvironmentManagerSettings
    {
        /// <summary>
        /// Gets or sets the default Max Environments Per Plan.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int DefaultMaxEnvironmentsPerPlan { get; set; }

        /// <summary>
        /// Gets or sets the default environment archive cutoff hours.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double DefaultEnvironmentArchiveCutoffHours { get; set; }

        /// <summary>
        /// Gets or sets the default environment archive batch size.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int DefaultEnvironmentArchiveBatchSize { get; set; }

        /// <summary>
        /// Gets or sets the default environment archive max active count.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int DefaultEnvironmentArchiveMaxActiveCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an environment archive enabled state.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool DefaultEnvironmentArchiveEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment failed worker shoudl be enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool DefaultEnvironmentFailedWorkerEnabled { get; set; }

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

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<double> EnvironmentArchiveCutoffHours(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<double>("setting:environment-archive-cuttoff-hours", logger, DefaultEnvironmentArchiveCutoffHours);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> EnvironmentArchiveBatchSize(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<int>("setting:environment-archive-batch-size", logger, DefaultEnvironmentArchiveBatchSize);
        }

        /// <summary>
        /// Gets or sets the max number of archive jobs allowed.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> EnvironmentArchiveMaxActiveCount(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<int>("setting:environment-archive-max-active-count", logger, DefaultEnvironmentArchiveMaxActiveCount);
        }

        /// <summary>
        /// Gets a value intdicating if static environment monitoring is enabled.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> StaticEnvironmentMonitoringEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:enable-static-environment-monitoring", logger, false);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentArchiveEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:environment-archive-enabled", logger, DefaultEnvironmentArchiveEnabled);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentFailedWorkerEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:environment-failed-worker-enabled", logger, DefaultEnvironmentFailedWorkerEnabled);
        }
    }
}
