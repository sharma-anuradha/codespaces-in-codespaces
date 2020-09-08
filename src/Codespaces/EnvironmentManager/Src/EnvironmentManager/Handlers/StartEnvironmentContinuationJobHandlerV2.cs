// <copyright file="StartEnvironmentContinuationJobHandlerV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Start Environment Continuation Job Handler. It can be either create or resume.
    /// </summary>
    public class StartEnvironmentContinuationJobHandlerV2 : EnvironmentContinuationJobHandlerBase<StartEnvironmentContinuationJobHandlerV2.StartEnvironmentContinuationInput, StartEnvironmentContinuationInputState, EnvironmentContinuationResult>
    {
        /// <summary>
        /// Default queue id.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-start-environment";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationJobHandlerV2"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">target env repo.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="workspaceManager">target workspace manager.</param>
        /// <param name="serviceProvider">target serviceProvider.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        /// <param name="environmentRepairWorkflows">Environment repair workflows.</param>
        /// <param name="jobQueueProducerFactory">Job Queue producer factory instance.</param>
        public StartEnvironmentContinuationJobHandlerV2(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IResourceAllocationManager resourceAllocationManager,
            IWorkspaceManager workspaceManager,
            IServiceProvider serviceProvider,
            IResourceSelectorFactory resourceSelector,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(cloudEnvironmentRepository, jobQueueProducerFactory)
        {
            ResourceBrokerHttpClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            ResourceAllocationManager = Requires.NotNull(resourceAllocationManager, nameof(resourceAllocationManager));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            ServiceProvider = Requires.NotNull(serviceProvider, nameof(serviceProvider));
            ResourceSelector = Requires.NotNull(resourceSelector, nameof(resourceSelector));

            Requires.NotNull(environmentRepairWorkflows, nameof(environmentRepairWorkflows));
            EnvironmentRepairWorkflows = environmentRepairWorkflows.ToDictionary(x => x.WorkflowType);
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerStartEnv;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Provisioning;

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IServiceProvider ServiceProvider { get; }

        private IResourceSelectorFactory ResourceSelector { get; }

        private Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        /// <inheritdoc/>
        protected override StartEnvironmentContinuationInputState GetStateFromPayload(StartEnvironmentContinuationInput payload)
        {
            return payload.CurrentState;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<StartEnvironmentContinuationInputState, EnvironmentContinuationResult>> ContinueAsync(StartEnvironmentContinuationInput payload, EnvironmentRecordRef record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Add environment id and resource ids to logger
            payload.LogResource(logger);

            if (StartEnvironmentContinuationHelpers.IsInvalidOrFailedState(record, payload))
            {
                return ReturnFailed($"FailedEnvironmentStartState record in invalid state '{record.Value.State}'");
            }

            // Run operation
            switch (payload.CurrentState)
            {
                case StartEnvironmentContinuationInputState.GetResource:
                    // Trigger get exisiting resources.
                    return ToContinuationInfo(await payload.RunGetResourceAsync(ResourceBrokerHttpClient, record, logger), payload);

                case StartEnvironmentContinuationInputState.AllocateResource:
                    // Trigger compute allocate by calling allocate endpoint
                    return ToContinuationInfo(await payload.RunAllocateResourceAsync(CloudEnvironmentRepository, ResourceSelector, ResourceAllocationManager, record, logger, LogBaseName), payload);

                case StartEnvironmentContinuationInputState.CheckResourceState:
                    // Trigger check resource state
                    return ToContinuationInfo(await payload.RunCheckResourceProvisioningAsync(CloudEnvironmentRepository, ResourceBrokerHttpClient, record, logger, LogBaseName), payload);

                case StartEnvironmentContinuationInputState.StartCompute:
                    // Trigger start compute by calling start endpoint
                    return ToContinuationInfo(await payload.RunStartComputeAsync(CloudEnvironmentRepository, EnvironmentStateManager, WorkspaceManager, ServiceProvider, record, logger, LogBaseName), payload);

                case StartEnvironmentContinuationInputState.CheckStartCompute:
                    // Check by calling start check endpoint
                    return ToContinuationInfo(await payload.RunCheckStartComputeAsync(ResourceBrokerHttpClient, record, logger), payload);

                case StartEnvironmentContinuationInputState.StartHeartbeatMonitoring:
                    // Start environment monitoring.
                    return ToContinuationInfo(await StartEnvironmentContinuationHelpers.RunStartEnvironmentMonitoring(ServiceProvider, record, logger), payload);

                default:
                    return ReturnFailed("InvalidEnvironmentCreateState");
            }
        }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(StartEnvironmentContinuationInput payload, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            switch (payload.ActionState)
            {
                case StartEnvironmentInputActionState.CreateNew:
                    return record.Value.Transitions.Provisioning;

                case StartEnvironmentInputActionState.Resume:
                    return record.Value.Transitions.Resuming;

                case StartEnvironmentInputActionState.Export:
                    return record.Value.Transitions.Exporting;

                default:
                    logger.LogErrorWithDetail($"{LogBaseName}_fetch_operation_transition_error", "Invalid operation transition");
                    return new TransitionState { Status = OperationState.Failed };
            }
        }

        /// <inheritdoc/>
        protected override async Task FailOperationCleanupCoreAsync(
            StartEnvironmentContinuationInput payload,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            logger.LogError($"{LogBaseName}_failed");

            if (payload.ActionState == StartEnvironmentInputActionState.CreateNew)
            {
                await payload.CleanResourcesAsync(CloudEnvironmentRepository, ResourceBrokerHttpClient, EnvironmentStateManager, WorkspaceManager, record, trigger, logger.NewChildLogger(), LogBaseName);
            }
            else
            {
                await ForceShutdownAsync(payload, record, trigger, logger.NewChildLogger());
            }
        }

        private Task ForceShutdownAsync(
            StartEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_force_suspend",
                async (childLogger) =>
                {
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(record.Value, childLogger);

                    await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
                },
                swallowException: true);
        }

        /// <summary>
        /// Continuation input type.
        /// </summary>
        public class StartEnvironmentContinuationInput : EnvironmentContinuationInputBase<StartEnvironmentContinuationInputState>, IStartEnvironmentContinuationPayloadV2
        {
            /// <inheritdoc/>
            public DateTime LastStateUpdated { get; set; }

            /// <inheritdoc/>
            public CloudEnvironmentOptions CloudEnvironmentOptions { get; set; }

            /// <inheritdoc/>
            [JsonConverter(typeof(CloudEnvironmentParametersConverter))]
            public CloudEnvironmentParameters CloudEnvironmentParameters { get; set; }

            /// <inheritdoc/>
            public new StartEnvironmentContinuationInputState CurrentState { get; set; }

            /// <inheritdoc/>
            public EnvironmentContinuationInputResource ComputeResource { get; set; }

            /// <inheritdoc/>
            public EnvironmentContinuationInputResource OSDiskResource { get; set; }

            /// <inheritdoc/>
            public EnvironmentContinuationInputResource StorageResource { get; set; }

            /// <inheritdoc/>
            public StartEnvironmentInputActionState ActionState { get; set; }
        }

        /// <summary>
        /// Json converter for CloudEnvironmentParameters type
        /// </summary>
        public class CloudEnvironmentParametersConverter : JsonTypeConverter
        {
            private static readonly Dictionary<string, Type> MapTypes
                    = new Dictionary<string, Type>
                {
                    { "export", typeof(ExportCloudEnvironmentParameters) },
                    { "start", typeof(StartCloudEnvironmentParameters) },
                };

            protected override Type BaseType => typeof(CloudEnvironmentParameters);

            protected override IDictionary<string, Type> SupportedTypes => MapTypes;
        }
    }
}
