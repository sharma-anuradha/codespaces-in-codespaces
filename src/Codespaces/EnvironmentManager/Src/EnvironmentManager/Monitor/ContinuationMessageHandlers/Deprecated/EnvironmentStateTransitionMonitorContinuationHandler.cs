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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

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
            ILatestHeartbeatMonitor latestHeartbeatMonitor,
            IServiceProvider serviceProvider,
            IEnvironmentSuspendAction environmentSuspendAction,
            IEnvironmentForceSuspendAction environmentForceSuspendAction,
            IEnvironmentFailAction environmentFailAction,
            VsoSuperuserClaimsIdentity superuserIdentity,
            ICurrentIdentityProvider currentIdentityProvider,
            EnvironmentMonitorSettings environmentMonitorSettings)
            : base(environmentRepository, latestHeartbeatMonitor, serviceProvider, environmentMonitorSettings)
        {
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
            EnvironmentForceSuspendAction = Requires.NotNull(environmentForceSuspendAction, nameof(environmentForceSuspendAction));
            EnvironmentFailAction = Requires.NotNull(environmentFailAction, nameof(EnvironmentFailAction));
            SuperuserIdentity = Requires.NotNull(superuserIdentity, nameof(superuserIdentity));
            CurrentIdentityProvider = Requires.NotNull(currentIdentityProvider, nameof(currentIdentityProvider));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        private IEnvironmentForceSuspendAction EnvironmentForceSuspendAction { get; }

        private IEnvironmentFailAction EnvironmentFailAction { get; }

        private VsoSuperuserClaimsIdentity SuperuserIdentity { get; }

        private ICurrentIdentityProvider CurrentIdentityProvider { get; }

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
                return CreateFinalResult(OperationState.Succeeded, EnvironmentMonitorConstants.HealthyStateTransitionReason);
            }

            if (environment.State == input.CurrentState)
            {
                // the recovery actions require the super user identity
                using (CurrentIdentityProvider.SetScopedIdentity(SuperuserIdentity))
                {
                    // the environment is still in the original state, so handle the timeout
                    return await HandleTimeoutAsync(input, environment, logger);
                }
            }

            // the state is neither the original nor target state, so stop monitoring
            return CreateFinalResult(OperationState.Cancelled, EnvironmentMonitorConstants.UnknownStateTransitionReason);
        }

        private async Task<ContinuationResult> HandleTimeoutAsync(EnvironmentStateTransitionInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                // no recovery is performed for static environments
                return CreateFinalResult(OperationState.Failed, EnvironmentMonitorConstants.TimeoutInStateTransitionReason);
            }

            switch (environment.State)
            {
                case CloudEnvironmentState.Starting:
                    // attempt to gracefully suspend this environment (which will kick off the suspend environment monitor as well)
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(environment.Id), false, logger.NewChildLogger());

                    // state transition has timed out, return next input
                    return CreateFinalResult(OperationState.Failed, EnvironmentMonitorConstants.TimeoutInStateTransitionReason);

                case CloudEnvironmentState.ShuttingDown:
                    // Timeout Kick off force shutdown to repair environment.
                    await EnvironmentForceSuspendAction.RunAsync(Guid.Parse(environment.Id), logger.NewChildLogger());

                    // state transition has timed out, return next input
                    return CreateFinalResult(OperationState.Failed, EnvironmentMonitorConstants.TimeoutInStateTransitionReason);

                case CloudEnvironmentState.Provisioning:
                    return await HandleProvisioningTimeoutAsync(input, environment, logger);

                case CloudEnvironmentState.Queued:
                    if (input.TargetState == CloudEnvironmentState.Provisioning)
                    {
                        // Timeout Kick off fail to cleanup environment.
                        await EnvironmentFailAction.RunAsync(Guid.Parse(environment.Id), EnvironmentMonitorConstants.TimeoutInStateTransitionReason, logger.NewChildLogger());
                    }
                    else
                    {
                        // Timeout Kick off force shutdown to repair environment.
                        await EnvironmentForceSuspendAction.RunAsync(Guid.Parse(environment.Id), logger.NewChildLogger());
                    }

                    // state transition has timed out, return next input
                    return CreateFinalResult(OperationState.Failed, EnvironmentMonitorConstants.TimeoutInStateTransitionReason);

                default:
                    return CreateFinalResult(OperationState.Cancelled, EnvironmentMonitorConstants.UnknownStateTransitionReason);
            }
        }

        private async Task<ContinuationResult> HandleProvisioningTimeoutAsync(EnvironmentStateTransitionInput input, CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            DateTime currentTimestamp = DateTime.UtcNow;

            if (environment.StateTimeout == null || currentTimestamp > environment.StateTimeout.Value)
            {
                // fail the environment because either 1.) the timeout has expired, or 2.) no job result was ever returned.
                await EnvironmentFailAction.RunAsync(Guid.Parse(environment.Id), EnvironmentMonitorConstants.TimeoutInStateTransitionReason, logger.NewChildLogger());

                // state transition has timed out, return next input
                return CreateFinalResult(OperationState.Failed, EnvironmentMonitorConstants.TimeoutInStateTransitionReason);
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
                return CreateFinalResult(OperationState.Succeeded, EnvironmentMonitorConstants.HealthyIntermediateStateTransitionReason);
            }
        }
    }
}