// <copyright file="EnvironmentStateTransitionMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
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
            logger.FluentAddBaseValue("EnvironmentTargetState", input.TargetState);
            logger.FluentAddBaseValue("EnvironmentTransitionTimeout", input.TransitionTimeout);

            if (environment.State == input.TargetState)
            {
                // the environment has reached the target state
                return CreateFinalResult(OperationState.Succeeded, "HealthyStateTransition");
            }

            if (environment.State == input.CurrentState)
            {
                // the environment is still in the original state, so handle the timeout
                return await HandleTimeoutAsync(input, environment, logger);
            }

            // the state is neither the original nor target state, so stop monitoring
            return CreateFinalResult(OperationState.Cancelled, "UnknownStateTransition");
        }

        private async Task<ContinuationResult> HandleTimeoutAsync(EnvironmentStateTransitionInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                // no recovery is performed for static environments
                return CreateFinalResult(OperationState.Failed, "TimeoutInStateTransition");
            }

            switch (environment.State)
            {
                case CloudEnvironmentState.Starting:
                case CloudEnvironmentState.ShuttingDown:
                    // Timeout Kick off force shutdown to repair environment.
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(environment, logger.NewChildLogger());

                    // state transition has timed out, return next input
                    return CreateFinalResult(OperationState.Failed, "TimeoutInStateTransition");

                case CloudEnvironmentState.Provisioning:
                    return await HandleProvisioningTimeoutAsync(input, environment, logger);

                case CloudEnvironmentState.Queued:
                    if (input.TargetState == CloudEnvironmentState.Provisioning)
                    {
                        // Timeout Kick off fail to cleanup environment.
                        await EnvironmentRepairWorkflows[EnvironmentRepairActions.Fail].ExecuteAsync(environment, logger.NewChildLogger());
                    }
                    else
                    {
                        // Timeout Kick off force shutdown to repair environment.
                        await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(environment, logger.NewChildLogger());
                    }

                    // state transition has timed out, return next input
                    return CreateFinalResult(OperationState.Failed, "TimeoutInStateTransition");

                default:
                    return CreateFinalResult(OperationState.Cancelled, "UnknownStateTransition");
            }
        }

        private async Task<ContinuationResult> HandleProvisioningTimeoutAsync(EnvironmentStateTransitionInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            DateTime currentTimestamp = DateTime.UtcNow;

            if (environment.StateTimeout == null || currentTimestamp > environment.StateTimeout.Value)
            {
                // fail the environment because either 1.) the timeout has expired, or 2.) no job result was ever returned.
                await EnvironmentRepairWorkflows[EnvironmentRepairActions.Fail].ExecuteAsync(environment, logger.NewChildLogger());

                // state transition has timed out, return next input
                return CreateFinalResult(OperationState.Failed, "TimeoutInStateTransition");
            }
            else
            {
                // configure the new timeout, using the value sent from the agent, and kick off the next monitor
                var newTimeout = environment.StateTimeout.Value - currentTimestamp;

                // using GetService to workaround a circular dependency issue
                var environmentMonitor = ServiceProvider.GetService<IEnvironmentMonitor>();

                // start a new continuation, rather than returning a continuation result due to the 1hr absolute timeout on continuations
                await environmentMonitor.MonitorProvisioningStateTransitionAsync(input.EnvironmentId, input.ComputeResourceId, newTimeout, logger.NewChildLogger());

                // the state transition is successful, so far
                return CreateFinalResult(OperationState.Succeeded, "HealthyIntermediateStateTransition");
            }
        }
    }
}