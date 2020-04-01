// <copyright file="EnvironmentStateTransitionMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment State Transition Monitor Continuation Handler.
    /// </summary>
    public class EnvironmentStateTransitionMonitorContinuationHandler : BaseEnvironmentMonitorContinuationHandler<EnvironmentStateTransitionInput>, IEnvironmentStateTransitionMonitorContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "EnvironmentStateTransitionMonitor";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentStateTransitionMonitorContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentRepository">target repo.</param>
        /// <param name="environmentRepairWorkflows">Target environment repair workflows.</param>
        /// <param name="latestHeartbeatMonitor">Target latest Heartbeat Monitor.</param>
        /// <param name="serviceProvider">Target environment service provider.</param>
        /// <param name="environmentMonitorSettings">Environment monitor settings.</param>
        public EnvironmentStateTransitionMonitorContinuationHandler(
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
        protected override ContinuationResult CreateContinuationResult(EnvironmentStateTransitionInput operationInput, IDiagnosticsLogger logger)
        {
            // push message to monitor state transition for an environment.
            return new ContinuationResult
            {
                Status = OperationState.InProgress,
                RetryAfter = operationInput.TransitionTimeout,
                NextInput = operationInput,
            };
        }

        /// <inheritdoc/>
        protected override async Task<bool> IsEnabledAsync(IDiagnosticsLogger logger)
        {
            return await EnvironmentMonitorSettings.EnableStateTransitionMonitoring(logger.NewChildLogger());
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(EnvironmentStateTransitionInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.State == input.TargetState)
            {
                // compute has healthy heartbeat, return next input
                return CreateFinalResult(OperationState.Succeeded, "HealthyStateTransition");
            }

            // Check environment state
            if (environment.State == input.CurrentState)
            {
                if (environment.State == CloudEnvironmentState.Starting || environment.State == CloudEnvironmentState.Unavailable)
                {
                    if (environment.Type != EnvironmentType.StaticEnvironment)
                    {
                        // Timeout Kick off force shutdown to repair environment.
                        await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(environment, logger.NewChildLogger());
                    }
                }

                // compute does not have a healthy heartbeat, return next input
                return CreateFinalResult(OperationState.Failed, "TimeoutInStateTransition");
            }

            // compute has healthy heartbeat, return next input
            return CreateFinalResult(OperationState.Cancelled, "UnknownStateTransition");
        }
    }
}