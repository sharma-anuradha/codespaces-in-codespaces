// <copyright file="StartEnvironmentContinuationHandlerV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Start Environment Continuation Handler V2. It can be either create, resume, or export.
    /// </summary>
    public class StartEnvironmentContinuationHandlerV2 :
         BaseContinuationTaskMessageHandler<StartEnvironmentContinuationInputV2>, IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandlerV2"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">target env repo.</param>
        /// <param name="heartbeatRepository">target env heartbeat repo.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="workspaceManager">target workspace manager.</param>
        /// <param name="serviceProvider">target serviceProvider.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        /// <param name="environmentRepairWorkflows">Environment repair workflows.</param>
        public StartEnvironmentContinuationHandlerV2(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IResourceAllocationManager resourceAllocationManager,
            IWorkspaceManager workspaceManager,
            IServiceProvider serviceProvider,
            IResourceSelectorFactory resourceSelector,
            IEnvironmentSuspendAction environmentSuspendAction)
            : base(cloudEnvironmentRepository)
        {
            HeartbeatRepository = Requires.NotNull(heartbeatRepository, nameof(heartbeatRepository));
            ResourceBrokerHttpClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            ServiceProvider = Requires.NotNull(serviceProvider, nameof(serviceProvider));
            ResourceSelector = Requires.NotNull(resourceSelector, nameof(resourceSelector));
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
        }

        /// <summary>
        /// Gets target name.
        /// </summary>
        public static string DefaultQueueTarget => "JobStartEnvironmentV2";

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerStartEnv;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Provisioning;

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IServiceProvider ServiceProvider { get; }

        private IResourceSelectorFactory ResourceSelector { get; }

        private ICloudEnvironmentHeartbeatRepository HeartbeatRepository { get; }

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Add environment id and resource ids to logger
            operationInput.LogResource(logger);

            if (StartEnvironmentContinuationHelpers.IsInvalidOrFailedState(record, operationInput))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = $"FailedEnvironmentStartState record in invalid state '{record.Value.State}'" };
            }

            // Run operation
            switch (operationInput.CurrentState)
            {
                case StartEnvironmentContinuationInputState.StartQueuedStateMonitor:
                    // Trigger start queued state transition monitor.
                    return await operationInput.RunStartQueuedStateMonitor(ServiceProvider, record, logger);

                case StartEnvironmentContinuationInputState.GetResource:
                    // Trigger get exisiting resources.
                    return await operationInput.RunGetResourceAsync(ResourceBrokerHttpClient, record, logger);

                case StartEnvironmentContinuationInputState.AllocateResource:
                    // Trigger compute allocate by calling allocate endpoint
                    return await operationInput.RunAllocateResourceAsync(CloudEnvironmentRepository, ResourceSelector, ResourceAllocationManager, record, logger, LogBaseName);

                case StartEnvironmentContinuationInputState.GetHeartbeatRecord:
                    // Trigger create environment heartbeat record if needed.
                    return await operationInput.RunGetHeartbeatRecordAsync(HeartbeatRepository, CloudEnvironmentRepository, record, logger, LogBaseName);
                    
                case StartEnvironmentContinuationInputState.CheckResourceState:
                    // Trigger check resource state
                    return await operationInput.RunCheckResourceProvisioningAsync(CloudEnvironmentRepository, ResourceBrokerHttpClient, record, logger, LogBaseName);
                
                case StartEnvironmentContinuationInputState.StartCompute:
                    // Trigger start compute by calling start endpoint
                    return await operationInput.RunStartComputeAsync(CloudEnvironmentRepository, EnvironmentStateManager, WorkspaceManager, ServiceProvider, record, logger, LogBaseName);

                case StartEnvironmentContinuationInputState.CheckStartCompute:
                    // Check by calling start check endpoint
                    return await operationInput.RunCheckStartComputeAsync(ResourceBrokerHttpClient, record, logger);

                case StartEnvironmentContinuationInputState.StartHeartbeatMonitoring:
                    // Start environment monitoring.
                    return await operationInput.RunStartEnvironmentMonitoring(ServiceProvider, record, logger);

                default:
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidEnvironmentCreateState" };
            }
        }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(StartEnvironmentContinuationInputV2 input, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            switch (input.ActionState)
            {
                case StartEnvironmentInputActionState.CreateNew:
                    return record.Value.Transitions.Provisioning;

                case StartEnvironmentInputActionState.Resume:
                    return record.Value.Transitions.Resuming;

                case StartEnvironmentInputActionState.Export:
                    return record.Value.Transitions.Exporting;

                case StartEnvironmentInputActionState.Update:
                    return record.Value.Transitions.Updating;

                default:
                    logger.LogErrorWithDetail($"{LogBaseName}_fetch_operation_transition_error", "Invalid operation transition");
                    return new TransitionState { Status = OperationState.Failed };
            }
        }

        /// <inheritdoc/>
        protected override Task<bool> FailOperationShouldTriggerCleanupAsync(
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> FailOperationCleanupCoreAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            logger.LogError($"{LogBaseName}_failed");

            if (operationInput.ActionState == StartEnvironmentInputActionState.CreateNew)
            {
                return await operationInput.CleanResourcesAsync(CloudEnvironmentRepository, ResourceBrokerHttpClient, EnvironmentStateManager, WorkspaceManager, record, trigger, logger.NewChildLogger(), LogBaseName);
            }
            else
            {
                return await ForceShutdownAsync(operationInput, record, trigger, logger.NewChildLogger());
            }
        }

        private async Task<ContinuationResult> ForceShutdownAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_force_suspend",
                async (childLogger) =>
                {
                    await EnvironmentSuspendAction.RunAsync(Guid.Parse(record.Value.Id), false, childLogger.NewChildLogger());

                    return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
                },
                swallowException: true);
        }
    }
}