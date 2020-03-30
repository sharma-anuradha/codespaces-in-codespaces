// <copyright file="CreateEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// <param name="serviceProvider">target serviceProvider.</param>
        public CreateEnvironmentContinuationHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IServiceProvider serviceProvider)
            : base(cloudEnvironmentRepository)
        {
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
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
                    return await RunAllocateComputeAsync(operationInput, record, logger);
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
            await FailEnvironmentAsync(record.Value, logger);
            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        private static void LogResource(CreateEnvironmentContinuationInput operationInput, IDiagnosticsLogger logger)
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

        private async Task<ContinuationResult> RunAllocateComputeAsync(
             CreateEnvironmentContinuationInput operationInput,
             EnvironmentRecordRef record,
             IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;

            var computeResult = cloudEnvironment.Compute;
            if (computeResult == null)
            {
                computeResult = await AllocateResourceAsync(operationInput, cloudEnvironment, ResourceType.ComputeVM, logger);
            }

            var storageResult = cloudEnvironment.Storage;
            if (storageResult == null)
            {
                storageResult = await AllocateResourceAsync(operationInput, cloudEnvironment, ResourceType.StorageFileShare, logger);
            }

            if (computeResult != null && storageResult != null)
            {
                // Setup result
                operationInput.ComputeResource = computeResult;
                operationInput.StorageResource = storageResult;
                operationInput.CurrentState = CreateEnvironmentContinuationInputState.CheckResourceState;

                LogResource(operationInput, logger);

                return new ContinuationResult
                {
                    NextInput = operationInput,
                    Status = OperationState.InProgress,
                };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidResourceAllocate" };
        }

        private async Task<ContinuationResult> RunCheckResourceProvisioningAsync(
            CreateEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;

            var computeStatus = await ResourceBrokerHttpClient.StatusAsync(operationInput.EnvironmentId, operationInput.ComputeResource.ResourceId, logger.NewChildLogger());
            var storageStatus = await ResourceBrokerHttpClient.StatusAsync(operationInput.EnvironmentId, operationInput.StorageResource.ResourceId, logger.NewChildLogger());

            operationInput.ComputeResource.IsReady = computeStatus.IsReady;
            operationInput.StorageResource.IsReady = storageStatus.IsReady;

            LogResource(operationInput, logger);

            if (computeStatus.IsReady && storageStatus.IsReady)
            {
                cloudEnvironment.Compute = operationInput.ComputeResource;
                cloudEnvironment.Storage = operationInput.StorageResource;
                await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger.NewChildLogger());
                operationInput.CurrentState = CreateEnvironmentContinuationInputState.StartCompute;
            }
            else if (CheckForFailedState(storageStatus.ProvisioningStatus))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStorageResourceState" };
            }
            else if (CheckForFailedState(computeStatus.ProvisioningStatus))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidComputeResourceState" };
            }

            return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
        }

        private async Task<ContinuationResult> RunStartComputeAsync(
        CreateEnvironmentContinuationInput operationInput,
        EnvironmentRecordRef record,
        IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;
            var environmentManager = ServiceProvider.GetService<IEnvironmentManager>();

            // Create the Live Share workspace
            cloudEnvironment.Connection = await environmentManager.CreateWorkspace(
                EnvironmentType.CloudEnvironment,
                cloudEnvironment.Id,
                cloudEnvironment.Compute.ResourceId,
                operationInput.StartCloudEnvironmentParameters.ConnectionServiceUri,
                cloudEnvironment.Connection?.ConnectionSessionPath,
                operationInput.StartCloudEnvironmentParameters.UserAuthToken,
                logger.NewChildLogger());

            if (string.IsNullOrWhiteSpace(cloudEnvironment.Connection.ConnectionSessionId))
            {
                logger.LogErrorWithDetail($"{LogBaseName}_resume_workspace_error", "Could not create the cloud environment workspace session.");

                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidCreateWorkspace" };
            }

            await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Provisioning, CloudEnvironmentStateUpdateTriggers.CreateEnvironment, string.Empty, logger.NewChildLogger());

            // Persist core cloud environment record
            cloudEnvironment = await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger.NewChildLogger());

            // Kick off start-compute before returning.
            var isSuccess = await environmentManager.StartComputeAsync(
                 cloudEnvironment,
                 cloudEnvironment.Compute.ResourceId,
                 cloudEnvironment.Storage.ResourceId,
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
                    RetryAfter = TimeSpan.FromSeconds(10),
                };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartCompute" };
        }

        private async Task<ContinuationResult> RunCheckStartComputeAsync(
            CreateEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var computeStatus = await ResourceBrokerHttpClient.StatusAsync(operationInput.EnvironmentId, operationInput.ComputeResource.ResourceId, logger.NewChildLogger());
            if (computeStatus.StartingStatus == OperationState.Succeeded)
            {
                operationInput.CurrentState = CreateEnvironmentContinuationInputState.StartHeartbeatMonitoring;
                return new ContinuationResult { Status = OperationState.InProgress, NextInput = operationInput, };
            }

            if (computeStatus.StartingStatus == OperationState.InProgress)
            {
                return new ContinuationResult { NextInput = operationInput, Status = OperationState.InProgress, };
            }

            await FailEnvironmentAsync(record.Value, logger.NewChildLogger());
            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartState" };
        }

        private async Task<ResourceAllocation> AllocateResourceAsync(
            CreateEnvironmentContinuationInput operationInput,
            CloudEnvironment cloudEnvironment,
            ResourceType resourceType,
            IDiagnosticsLogger logger)
        {
            // Setup request object
            var allocateRequest = new AllocateRequestBody
            {
                Type = resourceType,
                SkuName = cloudEnvironment.SkuName,
                Location = cloudEnvironment.Location,
                QueueCreateResource = true,
            };

            // Make request to allocate compute
            var allocateResponse = await ResourceBrokerHttpClient.AllocateAsync(
                operationInput.EnvironmentId, allocateRequest, logger.NewChildLogger());
            if (allocateResponse != null)
            {
                // Map across details
                return new ResourceAllocation
                {
                    ResourceId = allocateResponse.ResourceId,
                    SkuName = allocateResponse.SkuName,
                    Location = allocateResponse.Location,
                    Created = allocateResponse.Created,
                    Type = allocateResponse.Type,
                    IsReady = allocateResponse.IsReady,
                };
            }

            return null;
        }

        private async Task FailEnvironmentAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Failed, nameof(RunCheckResourceProvisioningAsync), "FailedToAllocateResource", logger.NewChildLogger());
            await CloudEnvironmentRepository.UpdateAsync(cloudEnvironment, logger.NewChildLogger());

            // Delete the allocated resources.
            if (cloudEnvironment.Compute != null)
            {
                await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(cloudEnvironment.Id), cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
            }

            if (cloudEnvironment.Storage != null)
            {
                await ResourceBrokerHttpClient.DeleteAsync(Guid.Parse(cloudEnvironment.Id), cloudEnvironment.Storage.ResourceId, logger.NewChildLogger());
            }
        }
    }
}