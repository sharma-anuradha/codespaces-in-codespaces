// <copyright file="EnvironmentMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment monitor operations.
    /// </summary>
    public class EnvironmentMonitor : IEnvironmentMonitor
    {
        private const string LogBaseName = "environment_monitor";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentMonitor"/> class.
        /// </summary>
        /// <param name="activator">Target activator.</param>
        /// <param name="environmentMonitorSettings">Target settings.</param>
        public EnvironmentMonitor(
             IContinuationTaskActivator activator,
             EnvironmentMonitorSettings environmentMonitorSettings)
        {
            Activator = activator;
            EnvironmentMonitorSettings = environmentMonitorSettings;
        }

        private IContinuationTaskActivator Activator { get; }

        private EnvironmentMonitorSettings EnvironmentMonitorSettings { get; }

        /// <inheritdoc/>
        public async Task MonitorHeartbeatAsync(string environmentId, Guid? computeId, IDiagnosticsLogger logger)
        {
            // Check for flighting switch
            if (!await EnvironmentMonitorSettings.EnableHeartbeatMonitoring(logger.NewChildLogger()))
            {
                // Stop environment monitoring
                return;
            }

            var loggingProperties = new Dictionary<string, string>();

            var input = new HeartbeatMonitorInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId ?? default(Guid),
                ContinuationToken = string.Empty,
            };

            var target = HeartbeatMonitorContinuationHandler.DefaultQueueTarget;

            var result = await Activator.Execute(target, input, logger, null, loggingProperties);
            if (result.Status != OperationState.InProgress)
            {
                throw new EnvironmentMonitorInitializationException(environmentId);
            }
        }

        /// <inheritdoc/>
        public async Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            // Check for flighting switch (second-level flag for provisioning state)
            if (!await EnvironmentMonitorSettings.EnableProvisioningStateTransitionMonitoring(logger.NewChildLogger()))
            {
                // Skip this monitor
                return;
            }

            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Provisioning,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.ProvisionEnvironmentAcknowledgementTimeoutInSeconds(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorProvisioningStateTransitionAsync(string environmentId, Guid computeId, TimeSpan timeout, IDiagnosticsLogger logger)
        {
            // Check for flighting switch (second-level flag for provisioning state)
            if (!await EnvironmentMonitorSettings.EnableProvisioningStateTransitionMonitoring(logger.NewChildLogger()))
            {
                // Skip this monitor
                return;
            }

            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Provisioning,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = timeout,
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorResumeStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Starting,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.ResumeEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorExportStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Exporting,
                TargetState = CloudEnvironmentState.Shutdown,
                TransitionTimeout = await EnvironmentMonitorSettings.ExportEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorShutdownStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.ShuttingDown,
                TargetState = CloudEnvironmentState.Shutdown,
                TransitionTimeout = await EnvironmentMonitorSettings.ShutdownEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task MonitorUnavailableStateTransitionAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
        {
            var input = new EnvironmentStateTransitionInput()
            {
                EnvironmentId = environmentId,
                ComputeResourceId = computeId,
                CurrentState = CloudEnvironmentState.Unavailable,
                TargetState = CloudEnvironmentState.Available,
                TransitionTimeout = await EnvironmentMonitorSettings.UnavailableEnvironmentTimeout(logger.NewChildLogger()),
            };

            await MonitorStateTransitionAsync(input, logger);
        }

        private Task MonitorStateTransitionAsync(EnvironmentStateTransitionInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_monitor_state_transition",
                async (childLogger) =>
                {
                    // Check for flighting switch
                    if (!await EnvironmentMonitorSettings.EnableStateTransitionMonitoring(childLogger.NewChildLogger()))
                    {
                        // Stop environment monitoring
                        return;
                    }

                    var loggingProperties = new Dictionary<string, string>();

                    var target = EnvironmentStateTransitionMonitorContinuationHandler.DefaultQueueTarget;

                    var result = await Activator.Execute(target, input, childLogger, null, loggingProperties);
                    if (result.Status != OperationState.InProgress)
                    {
                        throw new EnvironmentMonitorInitializationException(input.EnvironmentId);
                    }
                });
        }
    }
}
