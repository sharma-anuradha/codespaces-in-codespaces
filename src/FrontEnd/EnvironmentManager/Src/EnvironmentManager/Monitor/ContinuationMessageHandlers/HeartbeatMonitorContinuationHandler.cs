// <copyright file="HeartbeatMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// </summary>
    public class HeartbeatMonitorContinuationHandler : BaseEnvironmentMonitorContinuationHandler<HeartbeatMonitorInput>, IHeartbeatMonitorContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "HeartbeatMonitor";

        private static readonly List<CloudEnvironmentState> StateToProcess = new List<CloudEnvironmentState> { CloudEnvironmentState.Available, CloudEnvironmentState.Unavailable };

        private static readonly List<CloudEnvironmentState> StateToStopTracking = new List<CloudEnvironmentState> { CloudEnvironmentState.Shutdown, CloudEnvironmentState.Failed, CloudEnvironmentState.Deleted };

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartbeatMonitorContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentRepository">Target environment repository.</param>
        /// <param name="environmentRepairWorkflows">Target environment repair workflows.</param>
        /// <param name="latestHeartbeatMonitor">Target latest Heartbeat Monitor.</param>
        /// <param name="serviceProvider">Target environment service provider.</param>
        /// <param name="environmentMonitorSettings">Environment monitor settings.</param>
        public HeartbeatMonitorContinuationHandler(
            ICloudEnvironmentRepository environmentRepository,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows,
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IServiceProvider serviceProvider,
            EnvironmentMonitorSettings environmentMonitorSettings)
            : base(environmentRepository, environmentRepairWorkflows, latestHeartbeatMonitor, serviceProvider, environmentMonitorSettings)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ContinuationResult CreateContinuationResult(HeartbeatMonitorInput typedInput, IDiagnosticsLogger logger)
        {
            // push message to monitor heartbeat for new environment.
            return new ContinuationResult
            {
                Status = OperationState.InProgress,
                RetryAfter = TimeSpan.FromMinutes(EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes + EnvironmentMonitorConstants.BufferInMinutes),
                NextInput = typedInput,
            };
        }

        /// <inheritdoc/>
        protected override async Task<bool> IsEnabledAsync(IDiagnosticsLogger logger)
        {
            return await EnvironmentMonitorSettings.EnableHeartbeatMonitoring(logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(HeartbeatMonitorInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            // Check the Environment state is valid.
            if (StateToStopTracking.Contains(environment.State) || environment.Compute?.ResourceId == null)
            {
                // return result with null next input
                return CreateFinalResult(OperationState.Cancelled, "StopHeartbeatMonitoringState");
            }

            // Check environment state
            if (StateToProcess.Contains(environment.State))
            {
                // Check heartbeat timeout
                if (LatestHeartbeatMonitor.LastHeartbeatReceived != null
                    && LatestHeartbeatMonitor.LastHeartbeatReceived > environment.LastUpdatedByHeartBeat + TimeSpan.FromSeconds(EnvironmentMonitorConstants.HeartbeatIntervalInSeconds)
                    && environment.LastUpdatedByHeartBeat < DateTime.UtcNow.AddMinutes(-EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes))
                {
                    // Compute is not healthy, kick off force shutdown
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(environment, logger.NewChildLogger());

                    // return null next input
                    return CreateFinalResult(OperationState.Failed, "NoHeartbeat");
                }
            }

            // Start new continuation, as compute is healthy or State is transitioning.
            // Fetch instance
            var environmentMonitor = ServiceProvider.GetService<IEnvironmentMonitor>();

            // Starts the delete workflow on the resource
            await environmentMonitor.MonitorHeartbeatAsync(environment.Id, environment.Compute.ResourceId, logger.NewChildLogger());

            // compute has healthy heartbeat, return next input
            return CreateFinalResult(OperationState.Succeeded, "HealthyHeartbeat");
        }
    }
}
