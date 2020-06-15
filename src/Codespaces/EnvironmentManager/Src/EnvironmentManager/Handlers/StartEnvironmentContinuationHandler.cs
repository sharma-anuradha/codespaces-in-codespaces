// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
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
    /// Start Environment Continuation Handler. It can be either create or resume.
    /// </summary>
    public class StartEnvironmentContinuationHandler :
         BaseContinuationTaskMessageHandler<StartEnvironmentContinuationInput>, IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">target env repo.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="workspaceManager">target workspace manager.</param>
        /// <param name="serviceProvider">target serviceProvider.</param>
        /// <param name="resourceSelector">Resource selector.</param>
        /// <param name="environmentRepairWorkflows">Environment repair workflows.</param>
        public StartEnvironmentContinuationHandler(
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
        public static string DefaultQueueTarget => "JobStartEnvironment";

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
            StartEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            if (IsInvalidOrFailedState(record, operationInput))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = $"FailedEnvironmentStartState record in invalid state '{record.Value.State}'" };
            }

            // Add environment id and resource ids to logger
            LogResource(operationInput, logger);

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
        protected override TransitionState FetchOperationTransition(StartEnvironmentContinuationInput input, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            return input.CreateNew ?
                record.Value.Transitions.Provisioning :
                record.Value.Transitions.Resuming;
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
            StartEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            if (operationInput.CreateNew)
            {
                return await CleanResourcesAsync(operationInput, record, trigger, logger.NewChildLogger());
            }
            else
            {
                return await ForceShutdownAsync(operationInput, record, trigger, logger.NewChildLogger());
            }
        }

        private static void LogResource(
            StartEnvironmentContinuationInput operationInput,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ComputeResourceId", operationInput.ComputeResource?.ResourceId)
                .FluentAddBaseValue("ComputeResourceReady", operationInput.ComputeResource?.IsReady)
                .FluentAddBaseValue("StorageResourceId", operationInput.StorageResource?.ResourceId)
                .FluentAddBaseValue("StorageResourceReady", operationInput.StorageResource?.IsReady)
                .FluentAddBaseValue("OSDiskResourceId", operationInput.OSDiskResource?.ResourceId)
                .FluentAddBaseValue("OSDisResourceReady", operationInput.OSDiskResource?.IsReady)
                .AddBaseEnvironmentId(operationInput.EnvironmentId);
        }

        private static bool IsInvalidOrFailedState(EnvironmentRecordRef record, StartEnvironmentContinuationInput operationInput)
        {
            if (operationInput.CreateNew)
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
            StartEnvironmentContinuationInput operationInput,
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
            StartEnvironmentContinuationInput operationInput,
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

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        private async Task<ContinuationResult> RunGetResourceAsync(
         StartEnvironmentContinuationInput operationInput,
         EnvironmentRecordRef record,
         IDiagnosticsLogger logger)
        {
            if (operationInput.CreateNew)
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

            // Check if we got all the resources
            if (record.Value.OSDisk != default && osDiskStatus == default)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToGetOSDiskResource" };
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
             StartEnvironmentContinuationInput operationInput,
             EnvironmentRecordRef record,
             IDiagnosticsLogger logger)
        {
            var requests = await ResourceSelector.CreateAllocationRequestsAsync(
                record.Value,
                operationInput.CloudEnvironmentOptions,
                logger);

            var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                Guid.Parse(record.Value.Id),
                requests,
                logger.NewChildLogger());

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

        private async Task<bool> UpdateResourceInfoAsync(StartEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
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
                            // Update state to be failed
                            record.Value.Compute = computeResource;
                            if (hasOSDiskResource)
                            {
                                record.Value.OSDisk = osDiskResource;
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

        private async Task<ContinuationResult> RunCheckResourceProvisioningAsync(
            StartEnvironmentContinuationInput operationInput,
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

            operationInput.ComputeResource.IsReady = computeStatus.IsReady;

            if (hasOSDiskResource)
            {
                operationInput.OSDiskResource.IsReady = osDiskStatus.IsReady;
            }

            if (hasStorageResource)
            {
                operationInput.StorageResource.IsReady = storageStatus.IsReady;
            }

            LogResource(operationInput, logger);

            if (computeStatus.IsReady &&
                (!hasOSDiskResource || osDiskStatus.IsReady) &&
                (!hasStorageResource || storageStatus.IsReady))
            {
                bool didUpdate = await UpdateResourceInfoAsync(operationInput, record, logger);
                if (!didUpdate)
                {
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
                }

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
            StartEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Create the Live Share workspace
            var connection = await WorkspaceManager.CreateWorkspaceAsync(
                EnvironmentType.CloudEnvironment,
                record.Value.Id,
                record.Value.Compute.ResourceId,
                operationInput.StartCloudEnvironmentParameters.ConnectionServiceUri,
                record.Value.Connection?.ConnectionSessionPath,
                operationInput.StartCloudEnvironmentParameters.UserProfile.Email,
                operationInput.StartCloudEnvironmentParameters.UserAuthToken,
                logger.NewChildLogger());

            if (string.IsNullOrWhiteSpace(connection.ConnectionSessionId))
            {
                logger.LogErrorWithDetail($"{LogBaseName}_create_workspace_error", "Could not create the cloud environment workspace session.");

                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidCreateWorkspace" };
            }

            var didUpdate = await UpdateRecordAsync(
                    operationInput,
                    record,
                    async (environment, innerLogger) =>
                    {
                        // assign connection
                        environment.Connection = connection;

                        // Update state to be failed
                        await EnvironmentStateManager.SetEnvironmentStateAsync(
                            environment,
                            CloudEnvironmentState.Provisioning,
                            CloudEnvironmentStateUpdateTriggers.CreateEnvironment,
                            string.Empty,
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

            // Kick off start-compute before returning.
            var environmentManager = ServiceProvider.GetService<IEnvironmentManager>();
            var isSuccess = await environmentManager.StartComputeAsync(
                 record.Value,
                 record.Value.Compute.ResourceId,
                 record.Value.OSDisk?.ResourceId,
                 storageResourceId,
                 archiveStorageResourceId,
                 operationInput.CloudEnvironmentOptions,
                 operationInput.StartCloudEnvironmentParameters,
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
            StartEnvironmentContinuationInput operationInput,
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
    }
}