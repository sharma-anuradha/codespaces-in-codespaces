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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment monitor operations.
    /// </summary>
    public class EnvironmentMonitor : IEnvironmentMonitor
    {
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
        public async Task MonitorHeartbeatAsync(string environmentId, Guid computeId, IDiagnosticsLogger logger)
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
                ComputeResourceId = computeId,
                ContinuationToken = string.Empty,
            };

            var target = HeartbeatMonitorContinuationHandler.DefaultQueueTarget;

            var result = await Activator.Execute(target, input, logger, null, loggingProperties);
            if (result.Status != OperationState.InProgress)
            {
                throw new EnvironmentMonitorInitializationException(environmentId);
            }
        }
    }
}
