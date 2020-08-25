// <copyright file="StartEnvironmentContinuationHandlerV2.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.RepairWorkflows;
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
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="workspaceManager">target workspace manager.</param>
        /// <param name="serviceProvider">target serviceProvider.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        /// <param name="environmentRepairWorkflows">Environment repair workflows.</param>
        public StartEnvironmentContinuationHandlerV2(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IResourceAllocationManager resourceAllocationManager,
            IWorkspaceManager workspaceManager,
            IServiceProvider serviceProvider,
            IResourceSelectorFactory resourceSelector,
            IEnumerable<IEnvironmentRepairWorkflow> environmentRepairWorkflows)
            : base(cloudEnvironmentRepository)
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

        private Dictionary<EnvironmentRepairActions, IEnvironmentRepairWorkflow> EnvironmentRepairWorkflows { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Add environment id and resource ids to logger
            LogResource(operationInput, logger);

            if (IsInvalidOrFailedState(record, operationInput))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = $"FailedEnvironmentStartState record in invalid state '{record.Value.State}'" };
            }

            // Run operation
            switch (operationInput.CurrentState)
            {
                case StartEnvironmentContinuationInputState.GetResource:
                    // Trigger get exisiting resources.
                    return await RunGetResourceAsync(operationInput, record, logger);

                case StartEnvironmentContinuationInputState.AllocateResource:
                    // Trigger compute allocate by calling allocate endpoint
                    return await RunAllocateResourceAsync(operationInput, record, logger);

                case StartEnvironmentContinuationInputState.CheckResourceState:
                    // Trigger check resource state
                    return await RunCheckResourceProvisioningAsync(operationInput, record, logger);

                case StartEnvironmentContinuationInputState.StartCompute:
                    // Trigger start compute by calling start endpoint
                    return await RunStartComputeAsync(operationInput, record, logger);

                case StartEnvironmentContinuationInputState.CheckStartCompute:
                    // Check by calling start check endpoint
                    return await RunCheckStartComputeAsync(operationInput, record, logger);

                case StartEnvironmentContinuationInputState.StartHeartbeatMonitoring:
                    // Start environment monitoring.
                    return await RunStartEnvironmentMonitoring(record, logger);

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
                return await CleanResourcesAsync(operationInput, record, trigger, logger.NewChildLogger());
            }
            else
            {
                return await ForceShutdownAsync(operationInput, record, trigger, logger.NewChildLogger());
            }
        }

        private static void LogResource(
            StartEnvironmentContinuationInputV2 operationInput,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ComputeResourceId", operationInput.ComputeResource?.ResourceId)
                .FluentAddBaseValue("ComputeResourceReady", operationInput.ComputeResource?.IsReady)
                .FluentAddBaseValue("StorageResourceId", operationInput.StorageResource?.ResourceId)
                .FluentAddBaseValue("StorageResourceReady", operationInput.StorageResource?.IsReady)
                .FluentAddBaseValue("OSDiskResourceId", operationInput.OSDiskResource?.ResourceId)
                .FluentAddBaseValue("OSDisResourceReady", operationInput.OSDiskResource?.IsReady)
                .AddBaseEnvironmentId(operationInput.EnvironmentId)
                .FluentAddBaseValue(nameof(operationInput.CurrentState), operationInput.CurrentState)
                .FluentAddBaseValue(nameof(operationInput.ActionState), operationInput.ActionState);
        }

        private static bool IsInvalidOrFailedState(EnvironmentRecordRef record, StartEnvironmentContinuationInputV2 operationInput)
        {
            if (operationInput.ActionState == StartEnvironmentInputActionState.CreateNew)
            {
                return record.Value.State == CloudEnvironmentState.Failed;
            }
            else
            {
                return record.Value.State == CloudEnvironmentState.Deleted ||
                    record.Value.State == CloudEnvironmentState.ShuttingDown ||
                    record.Value.State == CloudEnvironmentState.Shutdown ||
                    record.Value.State == CloudEnvironmentState.Unavailable ||
                    record.Value.State == CloudEnvironmentState.Failed;
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
                    await EnvironmentRepairWorkflows[EnvironmentRepairActions.ForceSuspend].ExecuteAsync(record.Value, childLogger);

                    return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
                },
                swallowException: true);
        }

        private async Task<ContinuationResult> CleanResourcesAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var didUpdate = await UpdateRecordAsync(
                                operationInput,
                                record,
                                async (environment, innerLogger) =>
                                {
                                    // Update state to be failed
                                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                                                    environment,
                                                    CloudEnvironmentState.Failed,
                                                    nameof(FailOperationCleanupCoreAsync),
                                                    string.Empty,
                                                    null,
                                                    innerLogger);
                                    return true;
                                },
                                logger);

            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            var resourceList = new List<Guid>();

            // Delete the allocated resources.
            if (operationInput.ComputeResource != default)
            {
                resourceList.Add(operationInput.ComputeResource.ResourceId);
            }

            if (operationInput.StorageResource != default)
            {
                resourceList.Add(operationInput.StorageResource.ResourceId);
            }

            if (record.Value.OSDisk != default)
            {
                resourceList.Add(operationInput.OSDiskResource.ResourceId);
            }

            if (resourceList.Count != 0)
            {
                await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), resourceList, logger.NewChildLogger());
            }

            if (record.Value.Connection?.WorkspaceId != default)
            {
                await WorkspaceManager.DeleteWorkspaceAsync(record.Value.Connection.WorkspaceId, logger.NewChildLogger());
            }

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        private async Task<ContinuationResult> RunGetResourceAsync(
         StartEnvironmentContinuationInputV2 operationInput,
         EnvironmentRecordRef record,
         IDiagnosticsLogger logger)
        {
            if (operationInput.ActionState == StartEnvironmentInputActionState.CreateNew)
            {
                // Nothing to get.
                operationInput.CurrentState = StartEnvironmentContinuationInputState.AllocateResource;
                return new ContinuationResult
                {
                    NextInput = operationInput,
                    Status = OperationState.InProgress,
                };
            }

            // On resume there could be Storage file share and/or OS disk.
            // Just do a get operation on the items, and make sure the state is good.
            var resourceList = new List<Guid>();
            if (record.Value.OSDisk != default)
            {
                resourceList.Add(record.Value.OSDisk.ResourceId);
            }
            else if (record.Value.OSDiskSnapshot != default)
            {
                resourceList.Add(record.Value.OSDiskSnapshot.ResourceId);
            }

            if (record.Value.Storage != default)
            {
                resourceList.Add(record.Value.Storage.ResourceId);
            }

            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                resourceList,
                logger.NewChildLogger());

            var storageStatus = record.Value.Storage == default ? default : statusResponse.SingleOrDefault(x => x.Type == record.Value.Storage.Type);
            var osDiskStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var osDiskSnapshotStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.Snapshot);

            // Check if we got all the resources
            if (record.Value.OSDisk != default && osDiskStatus == default)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToGetOSDiskResource" };
            }
            else if (record.Value.OSDiskSnapshot != default && osDiskSnapshotStatus == default)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToGetOSDiskSnapshotResource" };
            }
            else if (record.Value.Storage != default && storageStatus == default)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToGetStorageResource" };
            }

            operationInput.CurrentState = StartEnvironmentContinuationInputState.AllocateResource;
            return new ContinuationResult
            {
                NextInput = operationInput,
                Status = OperationState.InProgress,
            };
        }

        private async Task<ContinuationResult> RunAllocateResourceAsync(
             StartEnvironmentContinuationInputV2 operationInput,
             EnvironmentRecordRef record,
             IDiagnosticsLogger logger)
        {
            var resultResponse = new List<ResourceAllocationRecord>();

            var requests = await ResourceSelector.CreateAllocationRequestsAsync(
                record.Value,
                logger);

            if (record.Value.OSDiskSnapshot != default)
            {
                // Make sure we don't get a OSDisk allocation request if resuming from a snapshot
                if (record.Value.OSDiskSnapshot != default && requests.Any(x => x.Type == ResourceType.OSDisk))
                {
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "UnexpectedOSDiskAllocationRequested" };
                }

                // When recovering from a snapshot, do separate request for the disk and compute
                var diskRequest = new AllocateRequestBody
                {
                    Type = ResourceType.OSDisk,
                    SkuName = record.Value.SkuName,
                    Location = record.Value.Location,
                    QueueCreateResource = true, // Create new custom resource
                    ExtendedProperties = new AllocateExtendedProperties { OSDiskSnapshotResourceID = record.Value.OSDiskSnapshot.ResourceId.ToString() },
                };

                var responses = await Task.WhenAll(
                    ResourceAllocationManager.AllocateResourcesAsync(
                        Guid.Parse(record.Value.Id),
                        new List<AllocateRequestBody> { diskRequest },
                        logger.NewChildLogger()),
                    ResourceAllocationManager.AllocateResourcesAsync(
                        Guid.Parse(record.Value.Id),
                        requests,
                        logger.NewChildLogger()));

                foreach (var response in responses)
                {
                    resultResponse.AddRange(response);
                }
            }
            else
            {
                resultResponse.AddRange(await ResourceAllocationManager.AllocateResourcesAsync(
                    Guid.Parse(record.Value.Id),
                    requests,
                    logger.NewChildLogger()));
            }

            var computeResponse = resultResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var osDiskResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var storageResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            // Setup result
            operationInput.ComputeResource = computeResponse.BuildQueueInputResource();
            operationInput.OSDiskResource = osDiskResponse.BuildQueueInputResource();
            operationInput.StorageResource = storageResponse.BuildQueueInputResource();

            bool didUpdate = await UpdateResourceInfoAsync(operationInput, record, logger);
            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            operationInput.CurrentState = StartEnvironmentContinuationInputState.CheckResourceState;

            LogResource(operationInput, logger);

            return new ContinuationResult
            {
                NextInput = operationInput,
                Status = OperationState.InProgress,
            };
        }

        private async Task<ContinuationResult> RunCheckResourceProvisioningAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var hasStorageResource = operationInput.StorageResource != default;
            var hasOSDiskResource = operationInput.OSDiskResource != default;

            var resourceList = new List<Guid>() { operationInput.ComputeResource.ResourceId };
            if (hasStorageResource)
            {
                resourceList.Add(operationInput.StorageResource.ResourceId);
            }

            if (hasOSDiskResource)
            {
                resourceList.Add(operationInput.OSDiskResource.ResourceId);
            }

            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                resourceList,
                logger.NewChildLogger());

            var computeStatus = statusResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var osDiskStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.OSDisk);
            var storageStatus = statusResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            var updatedResourceList = new List<Guid>();
            operationInput.ComputeResource = UpdateResourceStatus(computeStatus, operationInput.ComputeResource, updatedResourceList);

            if (hasOSDiskResource)
            {
                operationInput.OSDiskResource = UpdateResourceStatus(osDiskStatus, operationInput.OSDiskResource, updatedResourceList);
            }

            if (hasStorageResource)
            {
                operationInput.StorageResource = UpdateResourceStatus(storageStatus, operationInput.StorageResource, updatedResourceList);
            }

            LogResource(operationInput, logger);

            if (updatedResourceList.Count != 0)
            {
                try
                {
                    await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), updatedResourceList, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    // Continue on failure to delete shadow record, as it is best effort.
                    logger.LogException($"{LogBaseName}_delete_shadow_record_error", ex);
                }

                // Queued allocation request is completed, so update resource information in environment record.
                var didUpdate = await UpdateResourceInfoAsync(operationInput, record, logger.NewChildLogger());
                if (!didUpdate)
                {
                    // retry to update the updated resource in environment record.
                    return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
                }
            }

            if (statusResponse.All(status => status.IsReady))
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.StartCompute;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }
            else if (hasStorageResource && storageStatus.ProvisioningStatus.IsFailedState())
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStorageResourceState" };
            }
            else if (hasOSDiskResource && osDiskStatus.ProvisioningStatus.IsFailedState())
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidOSDiskResourceState" };
            }
            else if (computeStatus.ProvisioningStatus.IsFailedState())
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidComputeResourceState" };
            }

            return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, RetryAfter = TimeSpan.FromSeconds(10) };
        }

        private async Task<ContinuationResult> RunStartComputeAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var connection = new ConnectionInfo();

            // Set up liveshare workspace if user is not exporting
            if (operationInput.ActionState != StartEnvironmentInputActionState.Export)
            {
                StartCloudEnvironmentParameters cloudEnvironmentParameters = (StartCloudEnvironmentParameters)operationInput.CloudEnvironmentParameters;

                // Create the Live Share workspace
                connection = await WorkspaceManager.CreateWorkspaceAsync(
                    EnvironmentType.CloudEnvironment,
                    record.Value.Id,
                    record.Value.Compute.ResourceId,
                    cloudEnvironmentParameters.ConnectionServiceUri,
                    record.Value.Connection?.ConnectionSessionPath,
                    operationInput.CloudEnvironmentParameters.UserProfile.Email,
                    operationInput.CloudEnvironmentParameters.UserAuthToken,
                    logger.NewChildLogger());

                if (string.IsNullOrWhiteSpace(connection.ConnectionSessionId))
                {
                    logger.LogErrorWithDetail($"{LogBaseName}_create_workspace_error", "Could not create the cloud environment workspace session.");

                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidCreateWorkspace" };
                }
            }

            // Update state from queued
            var newState = operationInput.ActionState == StartEnvironmentInputActionState.CreateNew ? CloudEnvironmentState.Provisioning : CloudEnvironmentState.Starting;
            if (record.Value.State != CloudEnvironmentState.Queued)
            {
                logger.LogErrorWithDetail($"{LogBaseName}_invalid_state_error", $"Found invalid state {record.Value.State} instead of {CloudEnvironmentState.Queued}");

                if (record.Value.State == newState || record.Value.State == CloudEnvironmentState.Available || record.Value.State == CloudEnvironmentState.Unavailable)
                {
                    // Return success to cancel this continuation, as another continuation is already operating on this environment.
                    return new ContinuationResult { Status = OperationState.Succeeded };
                }
            }

            var didUpdate = await UpdateRecordAsync(
                    operationInput,
                    record,
                    async (environment, innerLogger) =>
                    {
                        // assign connection if environment is not exporting
                        if (operationInput.ActionState != StartEnvironmentInputActionState.Export)
                        {
                            environment.Connection = connection;
                        }

                        await EnvironmentStateManager.SetEnvironmentStateAsync(
                            environment,
                            newState,
                            CloudEnvironmentStateUpdateTriggers.CreateEnvironment,
                            string.Empty,
                            null,
                            logger.NewChildLogger());

                        return true;
                    },
                    logger);

            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            // Get archive storage id and storage resource id to start environment.
            var archiveStorageResourceId = (record.Value.Storage?.Type == ResourceType.StorageArchive) ? record.Value.Storage?.ResourceId : default;
            var storageResourceId = (record.Value.Storage?.Type == ResourceType.StorageFileShare) ? record.Value.Storage.ResourceId : operationInput.StorageResource?.ResourceId;

            // Get current action needed for starting compute
            var startEnvironmentAction = operationInput.ActionState == StartEnvironmentInputActionState.Export ? StartEnvironmentAction.StartExport : StartEnvironmentAction.StartCompute;

            // Kick off start-compute before returning.
            var environmentManager = ServiceProvider.GetService<IEnvironmentManager>();
            var isSuccess = await environmentManager.StartComputeAsync(
                 record.Value,
                 record.Value.Compute.ResourceId,
                 record.Value.OSDisk?.ResourceId,
                 storageResourceId,
                 archiveStorageResourceId,
                 operationInput.CloudEnvironmentParameters,
                 startEnvironmentAction,
                 logger.NewChildLogger());

            if (isSuccess)
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.CheckStartCompute;
                return new ContinuationResult
                {
                    NextInput = operationInput,
                    Status = OperationState.InProgress,
                    RetryAfter = TimeSpan.FromSeconds(1),
                };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartCompute" };
        }

        private async Task<ContinuationResult> RunCheckStartComputeAsync(
            StartEnvironmentContinuationInputV2 operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var computeStatus = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                operationInput.ComputeResource.ResourceId,
                logger.NewChildLogger());
            logger.AddBaseValue("ComputeStartingStatus", computeStatus.StartingStatus.ToString());

            if (computeStatus.StartingStatus == OperationState.Succeeded)
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.StartHeartbeatMonitoring;
                return new ContinuationResult { Status = OperationState.InProgress, NextInput = operationInput, };
            }

            if (computeStatus.StartingStatus == OperationState.InProgress || computeStatus.StartingStatus == OperationState.Initialized)
            {
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, RetryAfter = TimeSpan.FromSeconds(1) };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartComputeState" };
        }

        private async Task<ContinuationResult> RunStartEnvironmentMonitoring(
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;
            var environmentMonitor = ServiceProvider.GetService<IEnvironmentMonitor>();

            // Start Environment Monitoring
            await environmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());

            return new ContinuationResult { Status = OperationState.Succeeded };
        }

        private async Task<bool> UpdateResourceInfoAsync(StartEnvironmentContinuationInputV2 operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_update_resources_post_allocate",
                async (childLogger) =>
                {
                    var hasStorageResource = operationInput.StorageResource != default;
                    var hasOSDiskResource = operationInput.OSDiskResource != default;

                    var computeResource = operationInput.ComputeResource.BuildResourceRecord();
                    var osDiskResource = operationInput.OSDiskResource.BuildResourceRecord();
                    var storageResource = operationInput.StorageResource.BuildResourceRecord();

                    return await UpdateRecordAsync(
                        operationInput,
                        record,
                        (environment, innerLogger) =>
                        {
                            // Update compute and disk resources
                            record.Value.Compute = computeResource;
                            if (hasOSDiskResource)
                            {
                                record.Value.OSDisk = osDiskResource;
                                record.Value.OSDiskSnapshot = null;
                            }

                            // For archived environments, dont switch storage resource.
                            if (hasStorageResource && record.Value.Storage?.Type != ResourceType.StorageArchive)
                            {
                                record.Value.Storage = storageResource;
                            }

                            return Task.FromResult(true);
                        },
                        logger);
                });
        }

        private EnvironmentContinuationInputResource UpdateResourceStatus(
          StatusResponseBody resourceStatus,
          EnvironmentContinuationInputResource inputResource,
          List<Guid> shadowResourceList)
        {
            if (inputResource.ResourceId != resourceStatus.ResourceId)
            {
                shadowResourceList.Add(inputResource.ResourceId);
            }

            inputResource = resourceStatus.BuildQueueInputResource();

            return inputResource;
        }
    }
}