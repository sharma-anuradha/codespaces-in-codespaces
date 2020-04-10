// <copyright file="CreateEnvironmentContinuationHandler.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resume Environment Continuation Handler.
    /// </summary>
    public class CreateEnvironmentContinuationHandler :
         BaseContinuationTaskMessageHandler<CreateEnvironmentContinuationInput>, ICreateEnvironmentContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">target env repo.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        /// <param name="environmentStateManager">target environment state manager.</param>
        /// <param name="resourceAllocationManager">target resource allocation manager.</param>
        /// <param name="workspaceManager">target workspace manager.</param>
        /// <param name="serviceProvider">target serviceProvider.</param>
        public CreateEnvironmentContinuationHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IResourceAllocationManager resourceAllocationManager,
            IWorkspaceManager workspaceManager,
            IServiceProvider serviceProvider)
            : base(cloudEnvironmentRepository)
        {
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
            ResourceAllocationManager = resourceAllocationManager;
            WorkspaceManager = workspaceManager;
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets target name.
        /// </summary>
        public static string DefaultQueueTarget => "JobCreateEnvironment";

        /// <inheritdoc/>
        protected override string LogBaseName => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Provisioning;

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceAllocationManager ResourceAllocationManager { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IServiceProvider ServiceProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(
            CreateEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            if (record.Value.State == CloudEnvironmentState.Failed)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedEnvironmentCreateState" };
            }

            // Run operation
            switch (operationInput.CurrentState)
            {
                case CreateEnvironmentContinuationInputState.AllocateResource:
                    // Trigger compute allocate by calling allocate endpoint
                    return await RunAllocateResourceAsync(operationInput, record, logger);
                case CreateEnvironmentContinuationInputState.CheckResourceState:
                    // Trigger check resource state
                    return await RunCheckResourceProvisioningAsync(operationInput, record, logger);
                case CreateEnvironmentContinuationInputState.StartCompute:
                    // Trigger start compute by calling start endpoint
                    return await RunStartComputeAsync(operationInput, record, logger);
                case CreateEnvironmentContinuationInputState.CheckStartCompute:
                    // Check by calling start check endpoint
                    return await RunCheckStartComputeAsync(operationInput, record, logger);
                case CreateEnvironmentContinuationInputState.StartHeartbeatMonitoring:
                    // Start environment monitoring.
                    return await RunStartEnvironmentMonitoring(record, logger);
                default:
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidEnvironmentCreateState" };
            }
        }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(CreateEnvironmentContinuationInput input, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.Provisioning;
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
            CreateEnvironmentContinuationInput operationInput,
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
            if (record.Value.Compute != null)
            {
                resourceList.Add(record.Value.Compute.ResourceId);
            }

            if (record.Value.Storage != null)
            {
                resourceList.Add(record.Value.Storage.ResourceId);
            }

            if (resourceList.Count != 0)
            {
                await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), resourceList, logger.NewChildLogger());
            }

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        private static void LogResource(
            CreateEnvironmentContinuationInput operationInput,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ComputeResourceId", operationInput.ComputeResource?.ResourceId)
                .FluentAddBaseValue("ComputeResourceReady", operationInput.ComputeResource?.IsReady)
                .FluentAddBaseValue("StorageResourceId", operationInput.StorageResource?.ResourceId)
                .FluentAddBaseValue("StorageResourceReady", operationInput.StorageResource?.IsReady)
                .AddBaseEnvironmentId(operationInput.EnvironmentId);
        }

        private static bool CheckForFailedState(OperationState? resourceState)
        {
            return resourceState == null
                || resourceState == OperationState.Cancelled
                || resourceState == OperationState.Failed;
        }

        private static EnvironmentContinuationInputResource BuildQueueInputResource(ResourceAllocation resource)
        {
            return new EnvironmentContinuationInputResource()
            {
                Location = resource.Location,
                SkuName = resource.SkuName,
                Created = resource.Created,
                Type = resource.Type.Value,
                ResourceId = resource.ResourceId,
                IsReady = resource.IsReady,
            };
        }

        private static ResourceAllocation BuildResourceRecord(EnvironmentContinuationInputResource resource)
        {
            return new ResourceAllocation()
            {
                Location = resource.Location,
                SkuName = resource.SkuName,
                Created = resource.Created,
                Type = resource.Type,
                ResourceId = resource.ResourceId,
                IsReady = resource.IsReady,
            };
        }

        private async Task<ContinuationResult> RunAllocateResourceAsync(
             CreateEnvironmentContinuationInput operationInput,
             EnvironmentRecordRef record,
             IDiagnosticsLogger logger)
        {
            var computeRequest = new AllocateRequestBody
            {
                Type = ResourceType.ComputeVM,
                SkuName = record.Value.SkuName,
                Location = record.Value.Location,
                QueueCreateResource = operationInput.CloudEnvironmentOptions.QueueResourceAllocation,
            };

            var storageRequest = new AllocateRequestBody
            {
                Type = ResourceType.StorageFileShare,
                SkuName = record.Value.SkuName,
                Location = record.Value.Location,
                QueueCreateResource = false,
            };

            var inputRequest = new List<AllocateRequestBody> { computeRequest, storageRequest };

            var resultResponse = await ResourceAllocationManager.AllocateResourcesAsync(
                Guid.Parse(record.Value.Id),
                inputRequest,
                logger.NewChildLogger());

            var computeResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.ComputeVM);
            var storageResponse = resultResponse.SingleOrDefault(x => x.Type == ResourceType.StorageFileShare);

            // Setup result
            operationInput.ComputeResource = BuildQueueInputResource(computeResponse);
            operationInput.StorageResource = BuildQueueInputResource(storageResponse);
            operationInput.CurrentState = CreateEnvironmentContinuationInputState.CheckResourceState;

            LogResource(operationInput, logger);

            return new ContinuationResult
            {
                NextInput = operationInput,
                Status = OperationState.InProgress,
            };
        }

        private async Task<ContinuationResult> RunCheckResourceProvisioningAsync(
            CreateEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(operationInput.EnvironmentId, new List<Guid>() { operationInput.ComputeResource.ResourceId, operationInput.StorageResource.ResourceId }, logger.NewChildLogger());
            var computeStatus = statusResponse.Single(x => x.Type == ResourceType.ComputeVM);
            var storageStatus = statusResponse.Single(x => x.Type == ResourceType.StorageFileShare);
            operationInput.ComputeResource.IsReady = computeStatus.IsReady;
            operationInput.StorageResource.IsReady = storageStatus.IsReady;

            LogResource(operationInput, logger);

            if (computeStatus.IsReady && storageStatus.IsReady)
            {
                var computeResource = BuildResourceRecord(operationInput.ComputeResource);
                var storageResource = BuildResourceRecord(operationInput.StorageResource);

                var didUpdate = await UpdateRecordAsync(
                    operationInput,
                    record,
                    (environment, innerLogger) =>
                    {
                        // Update state to be failed
                        record.Value.Compute = computeResource;
                        record.Value.Storage = storageResource;
                        return Task.FromResult(true);
                    },
                    logger);

                if (!didUpdate)
                {
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
                }

                operationInput.CurrentState = CreateEnvironmentContinuationInputState.StartCompute;
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }
            else if (CheckForFailedState(storageStatus.ProvisioningStatus))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStorageResourceState" };
            }
            else if (CheckForFailedState(computeStatus.ProvisioningStatus))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidComputeResourceState" };
            }

            return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, RetryAfter = TimeSpan.FromSeconds(10) };
        }

        private async Task<ContinuationResult> RunStartComputeAsync(
            CreateEnvironmentContinuationInput operationInput,
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
                operationInput.StartCloudEnvironmentParameters.UserAuthToken,
                logger.NewChildLogger());

            if (string.IsNullOrWhiteSpace(connection.ConnectionSessionId))
            {
                logger.LogErrorWithDetail($"{LogBaseName}_resume_workspace_error", "Could not create the cloud environment workspace session.");

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

            // Kick off start-compute before returning.
            var environmentManager = ServiceProvider.GetService<IEnvironmentManager>();
            var isSuccess = await environmentManager.StartComputeAsync(
                 record.Value,
                 record.Value.Compute.ResourceId,
                 record.Value.Storage.ResourceId,
                 null,
                 operationInput.CloudEnvironmentOptions,
                 operationInput.StartCloudEnvironmentParameters,
                 logger.NewChildLogger());

            if (isSuccess)
            {
                operationInput.CurrentState = CreateEnvironmentContinuationInputState.CheckStartCompute;
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
            CreateEnvironmentContinuationInput operationInput,
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
                operationInput.CurrentState = CreateEnvironmentContinuationInputState.StartHeartbeatMonitoring;
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