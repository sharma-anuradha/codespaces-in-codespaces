// <copyright file="EnvironmentMonitorSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class EnvironmentMonitorSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether environment heatbeat monitor is enabled.
        /// </summary>
        public bool EnableEnvironmentHeartbeatMonitor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unavailable heartbeats are suspended.
        /// </summary>
        public bool EnableUnavailableEnvironmentHeartbeatMonitor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether environment state transition monitor is enabled.
        /// </summary>
        public bool EnableStateTransitionMonitor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether environment state transition monitor for provisioning is enabled.
        /// </summary>
        public bool EnableProvisioningStateTransitionMonitor { get; set; }

        /// <summary>
        /// Gets or sets the default timeout for provisoning acknowledgement.
        /// </summary>
        public int DefaultProvisionEnvironmentAcknowledgementTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets Resume Environment Timeout In Seconds.
        /// </summary>
        public int DefaultResumeEnvironmentTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets Export Environment Timeout In Seconds.
        /// </summary>
        public int DefaultExportEnvironmentTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets Shutdown Environment Timeout In Seconds.
        /// </summary>
        public int DefaultShutdownEnvironmentTimeoutInSeconds { get; set; }

        /// <summary>
        /// Gets or sets Unavailable Environment Timeout In Seconds.
        /// </summary>
        public int DefaultUnavailableEnvironmentTimeoutInSeconds { get; set; }

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
        /// Gets the resume environment timeout.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<TimeSpan> ResumeEnvironmentTimeout(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var timeout = await SystemConfiguration.GetValueAsync("setting:resume-environment-timeout-in-seconds", logger, DefaultResumeEnvironmentTimeoutInSeconds);
            return TimeSpan.FromSeconds(timeout);
        }

        /// <summary>
        /// Gets the export environment timeout.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<TimeSpan> ExportEnvironmentTimeout(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var timeout = await SystemConfiguration.GetValueAsync("setting:export-environment-timeout-in-seconds", logger, DefaultExportEnvironmentTimeoutInSeconds);
            return TimeSpan.FromSeconds(timeout);
        }

        /// <summary>
        /// Gets the provisioning acknowledgement timeout.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<TimeSpan> ProvisionEnvironmentAcknowledgementTimeoutInSeconds(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var timeout = await SystemConfiguration.GetValueAsync("setting:provision-environment-acknowledgement-timeout-in-seconds", logger, DefaultProvisionEnvironmentAcknowledgementTimeoutInSeconds);
            return TimeSpan.FromSeconds(timeout);
        }

        /// <summary>
        /// Gets the shutdown environment timeout.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<TimeSpan> ShutdownEnvironmentTimeout(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var timeout = await SystemConfiguration.GetValueAsync("setting:shutdown-environment-timeout-in-seconds", logger, DefaultShutdownEnvironmentTimeoutInSeconds);
            return TimeSpan.FromSeconds(timeout);
        }

        /// <summary>
        /// Gets the unavailable environment timeout.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public async Task<TimeSpan> UnavailableEnvironmentTimeout(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            var timeout = await SystemConfiguration.GetValueAsync("setting:unavailable-environment-timeout-in-seconds", logger, DefaultUnavailableEnvironmentTimeoutInSeconds);
            return TimeSpan.FromSeconds(timeout);
        }

        /// <summary>
        /// Get current flight switch for heartbeat monitoring.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target value.</returns>
        public Task<bool> EnableHeartbeatMonitoring(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:enable-environment-heartbeat-monitoring", logger, EnableEnvironmentHeartbeatMonitor);
        }

        /// <summary>
        /// Gets a value indicating whether to suspend unavailable environments from the hearbeat monitor.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <returns>Feature flag state.</returns>
        public Task<bool> EnableUnavailableEnvironmentHeartbeatMonitoring(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:enable-unavailable-environment-heartbeat-monitoring", logger, EnableUnavailableEnvironmentHeartbeatMonitor);
        }

        /// <summary>
        /// Get current flight switch for environment state transition monitoring.
        /// </summary>
        /// <param name="logger">target logger.</param>
        /// <returns>target value.</returns>
        public Task<bool> EnableStateTransitionMonitoring(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:enable-environment-state-transition-monitoring", logger, EnableStateTransitionMonitor);
        }

        /// <summary>
        /// Get current flight switch for provisioning environment state transition monitoring.
        /// </summary>
        /// <param name="logger">target logger.</param>
        /// <returns>target value.</returns>
        public Task<bool> EnableProvisioningStateTransitionMonitoring(IDiagnosticsLogger logger)
        {
            Requires.NotNull(SystemConfiguration, nameof(SystemConfiguration));

            return SystemConfiguration.GetValueAsync<bool>("featureflag:enable-environment-state-transition-monitoring-provisioning", logger, EnableProvisioningStateTransitionMonitor);
        }
    }
}
