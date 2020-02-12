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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Delete Orphaned Resource Handler.
    /// </summary>
    public class HeartbeatMonitorContinuationHandler : IHeartbeatMonitorContinuationHandler
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
        {
            EnvironmentRepository = Requires.NotNull(environmentRepository, nameof(environmentRepository));
            EnvironmentMonitorSettings = Requires.NotNull(environmentMonitorSettings, nameof(environmentMonitorSettings));
            EnvironmentRepairWorkflows = environmentRepairWorkflows.ToDictionary(x => x.WorkflowType);
            LatestHeartbeatMonitor = latestHeartbeatMonitor;
            ServiceProvider = serviceProvider;
        }

        private ILatestHeartbeatMonitor LatestHeartbeatMonitor { get; }

        private IServiceProvider ServiceProvider { get; }

        private ICloudEnvironmentRepository EnvironmentRepository { get; }

        private EnvironmentMonitorSettings EnvironmentMonitorSettings { get; }

        private Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        /// <inheritdoc/>
        public bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultQueueTarget;
        }

        /// <inheritdoc/>
        public async Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            var typedInput = input as HeartbeatMonitorInput;

            // Check for flighting switch
            if (!await EnvironmentMonitorSettings.EnableHeartbeatMonitoring(logger.NewChildLogger()))
            {
                // Stop environment monitoring
                return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Cancelled, "EnvironmentMonitoringDisabled");
            }

            // Start Environment Hearbeat monitoring
            if (string.IsNullOrEmpty(typedInput.ContinuationToken))
            {
                typedInput.ContinuationToken = typedInput.EnvironmentId;

                // push message to monitor heartbeat for new environment.
                return EnvironmentMonitorResultBuilder.CreateHeartbeatContinuationResult(typedInput, DateTime.UtcNow);
            }

            // Get env record
            var envRecord = await EnvironmentRepository.GetAsync(typedInput.EnvironmentId, logger.NewChildLogger());

            if (envRecord == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                // return result with null next input
                return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Cancelled, "EnvironmentRecordNotFound");
            }

            // Check the Environment state is valid.
            if (StateToStopTracking.Contains(envRecord.State))
            {
                // return result with null next input
                return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Cancelled, "StopHeartbeatMonitoringState");
            }

            // Check Compute Id matches with message
            if (envRecord.Compute?.ResourceId != null && envRecord.Compute.ResourceId != typedInput.ComputeResourceId)
            {
                // return result with null next input
                return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Cancelled, "EnvironmentResourceChanged");
            }

            // Check environment state
            if (StateToProcess.Contains(envRecord.State))
            {
                // Check heartbeat timeout
                if (LatestHeartbeatMonitor.LastHeartbeatReceived != null
                    && LatestHeartbeatMonitor.LastHeartbeatReceived > envRecord.LastUpdatedByHeartBeat + TimeSpan.FromSeconds(EnvironmentMonitorConstants.HeartbeatIntervalInSeconds)
                    && envRecord.LastUpdatedByHeartBeat < DateTime.UtcNow.AddMinutes(-EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes))
                {
                    // Compute is not healthy, kick off force shutdown
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(envRecord, logger.NewChildLogger());

                    // return null next input
                    return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Failed, "NoHeartbeat");
                }
            }

            // Start new continuation, as compute is healthy or State is transitioning.
            // Fetch instance
            var environmentMonitor = ServiceProvider.GetService<IEnvironmentMonitor>();

            // Starts the delete workflow on the resource
            await environmentMonitor.MonitorHeartbeatAsync(envRecord.Id, envRecord.Compute.ResourceId, logger.NewChildLogger());

            // compute has healthy hearbeat, return next input
            return EnvironmentMonitorResultBuilder.CreateFinalResult(OperationState.Succeeded, "HealthyHeartbeat");
        }
    }
}
