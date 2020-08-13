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
        /// Gets or sets the default suspended environment archive cutoff hours.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double DefaultSuspendedEnvironmentArchiveCutoffHours { get; set; }

        /// <summary>
        /// Gets or sets the default soft-deleted environment archive cutoff hours.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double DefaultSoftDeletedEnvironmentArchiveCutoffHours { get; set; }

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
        /// Gets or sets the default soft deleted environment hard delete cutoff hours.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public double DefaultEnvironmentHardDeleteCutoffHours { get; set; }

        /// Gets or sets a value indicating whether OS disk archiving is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool DefaultEnvironmentOSDiskArchiveEnabled { get; set; }

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

        /// <summary>
        /// Gets or sets a value indicating whether if the environment per plan check is enabled.
        /// </summary>
        public bool DefaultComputeCheckEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Windows OS compute quota check is enabled.
        /// </summary>
        public bool DefaultWindowsComputeCheckEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the environment soft delete is enabled.
        /// </summary>
        public bool DefaultEnvironmentSoftDeleteEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether environments' dynamic archival time should be set or not.
        /// </summary>
        public bool DefaultDynamicEnvironmentArchivalTimeEnabled { get; set; }

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
        public async Task<int> MaxEnvironmentsPerPlanAsync(string subscriptionId, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscriptionId, nameof(subscriptionId));
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var globalLimit = await SystemConfiguration.GetValueAsync("quota:max-environments-per-plan", logger, DefaultMaxEnvironmentsPerPlan);
            var subscriptionLimit = await SystemConfiguration.GetSubscriptionValueAsync("quota:max-environments-per-plan", subscriptionId, logger, globalLimit);

            return subscriptionLimit;
        }

        /// <summary>
        /// Gets or sets the Suspended Environment Archive Cutoff Hours.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<double> SuspendedEnvironmentArchiveCutoffHours(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("setting:environment-archive-cuttoff-hours", logger, DefaultSuspendedEnvironmentArchiveCutoffHours);
        }

        /// <summary>
        /// Gets or sets the Soft Deleted Environment Archive Cutoff Hours.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<double> SoftDeletedEnvironmentArchiveCutoffHours(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("setting:deleted-environment-archive-cuttoff-hours", logger, DefaultSoftDeletedEnvironmentArchiveCutoffHours);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> EnvironmentArchiveBatchSize(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("setting:environment-archive-batch-size", logger, DefaultEnvironmentArchiveBatchSize);
        }

        /// <summary>
        /// Gets or sets the max number of archive jobs allowed.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<int> EnvironmentArchiveMaxActiveCount(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("setting:environment-archive-max-active-count", logger, DefaultEnvironmentArchiveMaxActiveCount);
        }

        /// <summary>
        /// Gets or sets the Soft Deleted Environment Full Delete Cutoff Hours.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<double> EnvironmentHardDeleteCutoffHours(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("setting:soft-deleted-environment-terminate-cuttoff-hours", logger, DefaultEnvironmentHardDeleteCutoffHours);
        }

        /// <summary>
        /// Gets a value intdicating if static environment monitoring is enabled.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> StaticEnvironmentMonitoringEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:enable-static-environment-monitoring", logger, false);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentArchiveEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:environment-archive-enabled", logger, DefaultEnvironmentArchiveEnabled);
        }

        /// <summary>
        /// Gets whether OS disk archiving is enabled.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentOSDiskArchiveEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:environment-osdisk-archive-enabled", logger, DefaultEnvironmentOSDiskArchiveEnabled);
        }

        /// <summary>
        /// Gets or sets the Max Environments Per Plan.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentFailedWorkerEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:environment-failed-worker-enabled", logger, DefaultEnvironmentFailedWorkerEnabled);
        }

        /// <summary>
        /// Gets or sets the Compute Check.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> ComputeCheckEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:compute-check-enabled", logger, DefaultComputeCheckEnabled);
        }

        /// <summary>
        /// Gets or sets the Windows Compute Check.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> WindowsComputeCheckEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:windows-compute-check-enabled", logger, DefaultWindowsComputeCheckEnabled);
        }

        /// <summary>
        /// Gets or sets the environment soft delete feature flag.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnvironmentSoftDeleteEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:environment-soft-delete-enabled", logger, DefaultEnvironmentSoftDeleteEnabled);
        }

        /// <summary>
        /// Gets or sets a value indicating whether environments' dynamic archival time should be set or not.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> DynamicEnvironmentArchivalTimeEnabled(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync("featureflag:dynamic-environment-archival-time-enabled", logger, DefaultDynamicEnvironmentArchivalTimeEnabled);
        }
    }
}
