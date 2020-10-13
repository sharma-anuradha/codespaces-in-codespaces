// <copyright file="StartEnvironmentContinuationHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Start env continuation helpers.
    /// </summary>
    internal static class StartEnvironmentContinuationHelpers
    {
#pragma warning disable SA1600 // Elements should be documented

        public static void LogResource(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
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

        public static bool IsInvalidOrFailedState(IEntityRecordRef<CloudEnvironment> record, IStartEnvironmentContinuationPayloadV2 operationInput)
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

        public static async Task<ContinuationResult> RunStartQueuedStateMonitor(
             this IStartEnvironmentContinuationPayloadV2 operationInput,
             IServiceProvider serviceProvider,
             IEntityRecordRef<CloudEnvironment> record,
             IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;
            var environmentMonitor = serviceProvider.GetService<IEnvironmentMonitor>();
            var targetState = GetTargetState(operationInput.ActionState);

            // Start Environment Monitoring
            await environmentMonitor.MonitorQueuedStateTransitionAsync(cloudEnvironment.Id, targetState, logger.NewChildLogger());

            operationInput.CurrentState = StartEnvironmentContinuationInputState.GetResource;

            return ContinuationResultHelpers.ReturnInProgress(operationInput);
        }

        public static async Task<ContinuationResult> RunGetResourceAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            if (operationInput.ActionState == StartEnvironmentInputActionState.CreateNew)
            {
                // Nothing to get.
                operationInput.CurrentState = StartEnvironmentContinuationInputState.AllocateResource;
                return ContinuationResultHelpers.ReturnInProgress(operationInput);
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

            var statusResponse = await resourceBrokerHttpClient.StatusAsync(
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
            return ContinuationResultHelpers.ReturnInProgress(operationInput);
        }

        public static async Task<ContinuationResult> RunAllocateResourceAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceSelectorFactory resourceSelector,
            IResourceAllocationManager resourceAllocationManager,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            var resultResponse = new List<ResourceAllocationRecord>();

            var requests = await resourceSelector.CreateAllocationRequestsAsync(
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
                    resourceAllocationManager.AllocateResourcesAsync(
                        Guid.Parse(record.Value.Id),
                        new List<AllocateRequestBody> { diskRequest },
                        logger.NewChildLogger()),
                    resourceAllocationManager.AllocateResourcesAsync(
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
                resultResponse.AddRange(await resourceAllocationManager.AllocateResourcesAsync(
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

            bool didUpdate = await UpdateResourceInfoAsync(operationInput, cloudEnvironmentRepository, record, logger, operationBaseName);
            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            operationInput.CurrentState = StartEnvironmentContinuationInputState.GetHeartbeatRecord;

            LogResource(operationInput, logger);

            return ContinuationResultHelpers.ReturnInProgress(operationInput);
        }

        public static async Task<ContinuationResult> RunGetHeartbeatRecordAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            if (string.IsNullOrEmpty(record.Value.HeartbeatResourceId))
            {
                var heartbeatRecord = new CloudEnvironmentHeartbeat();
                heartbeatRecord = await heartbeatRepository.CreateAsync(heartbeatRecord, logger.NewChildLogger());

                var didUpdate = await cloudEnvironmentRepository.UpdateRecordAsync(
                                    operationInput.EnvironmentId,
                                    record,
                                    (environment, innerLogger) =>
                                    {
                                        // Update heartbeat record id.
                                        record.Value.HeartbeatResourceId = heartbeatRecord.Id;

                                        return Task.FromResult(true);
                                    },
                                    logger,
                                    operationBaseName);

                if (!didUpdate)
                {
                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
                }
            }

            operationInput.CurrentState = StartEnvironmentContinuationInputState.CheckResourceState;

            return ContinuationResultHelpers.ReturnInProgress(operationInput);
        }

        public static async Task<ContinuationResult> RunCheckResourceProvisioningAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            string operationBaseName)
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

            var statusResponse = await resourceBrokerHttpClient.StatusAsync(
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
                    await resourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), updatedResourceList, logger.NewChildLogger());
                }
                catch (Exception ex)
                {
                    // Continue on failure to delete shadow record, as it is best effort.
                    logger.LogException($"{operationBaseName}_delete_shadow_record_error", ex);
                }

                // Queued allocation request is completed, so update resource information in environment record.
                var didUpdate = await UpdateResourceInfoAsync(operationInput, cloudEnvironmentRepository, record, logger.NewChildLogger(), operationBaseName);
                if (!didUpdate)
                {
                    // retry to update the updated resource in environment record.
                    return ContinuationResultHelpers.ReturnInProgress(operationInput);
                }
            }

            if (statusResponse.All(status => status.IsReady))
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.StartCompute;
                return ContinuationResultHelpers.ReturnInProgress(operationInput);
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

            return ContinuationResultHelpers.ReturnInProgress(operationInput, TimeSpan.FromSeconds(10));
        }

        public static async Task<ContinuationResult> RunStartComputeAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentStateManager environmentStateManager,
            IWorkspaceManager workspaceManager,
            IServiceProvider serviceProvider,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            var connection = new ConnectionInfo();

            // Set up liveshare workspace if user is not exporting or not passing a UserProfile
            if (operationInput.ShouldEstablishWorkspaceConnection())
            {
                StartCloudEnvironmentParameters cloudEnvironmentParameters = (StartCloudEnvironmentParameters)operationInput.CloudEnvironmentParameters;

                // Create the Live Share workspace
                connection = await workspaceManager.CreateWorkspaceAsync(
                    EnvironmentType.CloudEnvironment,
                    record.Value.Id,
                    record.Value.Compute.ResourceId,
                    cloudEnvironmentParameters.ConnectionServiceUri,
                    record.Value.Connection?.ConnectionSessionPath,
                    operationInput.CloudEnvironmentParameters.UserProfile.Email,
                    operationInput.CloudEnvironmentParameters.UserProfile.Id,
                    record.Value.SkuName.Contains("windows", StringComparison.OrdinalIgnoreCase),
                    operationInput.CloudEnvironmentParameters.UserAuthToken,
                    logger.NewChildLogger());

                if (string.IsNullOrWhiteSpace(connection.ConnectionSessionId))
                {
                    logger.LogErrorWithDetail($"{operationBaseName}_create_workspace_error", "Could not create the cloud environment workspace session.");

                    return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidCreateWorkspace" };
                }
            }

            if (record.Value.State != CloudEnvironmentState.Queued)
            {
                logger.AddCloudEnvironmentState(record.Value.State)
                    .LogErrorWithDetail($"{operationBaseName}_invalid_state_error", $"Found invalid state {record.Value.State} instead of {CloudEnvironmentState.Queued}");

                // Return success to cancel this continuation, as another continuation is already operating on this environment.
                return new ContinuationResult { Status = OperationState.Cancelled, ErrorReason = $"InvalidState_{record.Value.State}" };
            }

            // Update state from queued
            var targetState = GetTargetState(operationInput.ActionState);

            var didUpdate = await cloudEnvironmentRepository.UpdateRecordAsync(
                    operationInput.EnvironmentId,
                    record,
                    async (environment, innerLogger) =>
                    {
                        // assign connection if environment is not exporting or not providing a UserProfile
                        if (operationInput.ShouldEstablishWorkspaceConnection())
                        {
                            environment.Connection = connection;
                        }

                        await environmentStateManager.SetEnvironmentStateAsync(
                            environment,
                            targetState,
                            CloudEnvironmentStateUpdateTriggers.CreateEnvironment,
                            string.Empty,
                            null,
                            logger.NewChildLogger());

                        return true;
                    },
                    logger,
                    operationBaseName);

            if (!didUpdate)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedToUpdateEnvironmentRecord" };
            }

            // Get archive storage id and storage resource id to start environment.
            var archiveStorageResourceId = (record.Value.Storage?.Type == ResourceType.StorageArchive) ? record.Value.Storage?.ResourceId : default;
            var storageResourceId = (record.Value.Storage?.Type == ResourceType.StorageFileShare) ? record.Value.Storage.ResourceId : operationInput.StorageResource?.ResourceId;

            // Get current action needed for starting compute
            var startEnvironmentAction = operationInput.ActionState switch
            {
                StartEnvironmentInputActionState.Export => StartEnvironmentAction.StartExport,
                StartEnvironmentInputActionState.Update => StartEnvironmentAction.StartUpdate,
                _ => StartEnvironmentAction.StartCompute
            };

            // Kick off start-compute before returning.
            var environmentManager = serviceProvider.GetService<IEnvironmentManager>();
            var isSuccess = await environmentManager.StartComputeAsync(
                 record.Value,
                 record.Value.Compute.ResourceId,
                 record.Value.OSDisk?.ResourceId,
                 storageResourceId,
                 archiveStorageResourceId,
                 operationInput.CloudEnvironmentOptions,
                 operationInput.CloudEnvironmentParameters,
                 startEnvironmentAction,
                 logger.NewChildLogger());

            if (isSuccess)
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.CheckStartCompute;
                return ContinuationResultHelpers.ReturnInProgress(operationInput, TimeSpan.FromSeconds(1));
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartCompute" };
        }

        public static async Task<ContinuationResult> RunCheckStartComputeAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            var computeStatus = await resourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId,
                operationInput.ComputeResource.ResourceId,
                logger.NewChildLogger());
            logger.AddBaseValue("ComputeStartingStatus", computeStatus.StartingStatus.ToString());

            if (computeStatus.StartingStatus == OperationState.Succeeded)
            {
                operationInput.CurrentState = StartEnvironmentContinuationInputState.StartHeartbeatMonitoring;
                return ContinuationResultHelpers.ReturnInProgress(operationInput);
            }

            if (computeStatus.StartingStatus == OperationState.InProgress || computeStatus.StartingStatus == OperationState.Initialized)
            {
                return ContinuationResultHelpers.ReturnInProgress(operationInput, TimeSpan.FromSeconds(1));
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidStartComputeState" };
        }

        public static async Task<ContinuationResult> RunStartEnvironmentMonitoring(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            IServiceProvider serviceProvider,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger)
        {
            var cloudEnvironment = record.Value;
            var environmentMonitor = serviceProvider.GetService<IEnvironmentMonitor>();

            // Start Environment Monitoring
            await environmentMonitor.MonitorHeartbeatAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());

            switch (operationInput.ActionState)
            {
                case StartEnvironmentInputActionState.CreateNew:
                    await environmentMonitor.MonitorProvisioningStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
                    break;
                case StartEnvironmentInputActionState.Resume:
                    await environmentMonitor.MonitorResumeStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
                    break;
                case StartEnvironmentInputActionState.Export:
                    await environmentMonitor.MonitorExportStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
                    break;
                case StartEnvironmentInputActionState.Update:
                    await environmentMonitor.MonitorUpdateStateTransitionAsync(cloudEnvironment.Id, cloudEnvironment.Compute.ResourceId, logger.NewChildLogger());
                    break;
                default:
                    throw new ArgumentException($"{operationInput.ActionState} value is not valid.", nameof(operationInput.ActionState));
            }

            return new ContinuationResult { Status = OperationState.Succeeded };
        }

        public static async Task<ContinuationResult> CleanResourcesAsync(
            this IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IEnvironmentStateManager environmentStateManager,
            IWorkspaceManager workspaceManager,
            IEntityRecordRef<CloudEnvironment> record,
            string trigger,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            var didUpdate = await cloudEnvironmentRepository.UpdateRecordAsync(
                                operationInput.EnvironmentId,
                                record,
                                async (environment, innerLogger) =>
                                {
                                    // Update state to be failed
                                    await environmentStateManager.SetEnvironmentStateAsync(
                                                    environment,
                                                    CloudEnvironmentState.Failed,
                                                    "FailOperationCleanupCoreAsync",
                                                    string.Empty,
                                                    null,
                                                    innerLogger);
                                    return true;
                                },
                                logger,
                                operationBaseName);

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
                await resourceBrokerHttpClient.DeleteAsync(Guid.Parse(record.Value.Id), resourceList, logger.NewChildLogger());
            }

            if (record.Value.Connection?.WorkspaceId != default)
            {
                await workspaceManager.DeleteWorkspaceAsync(record.Value.Connection.WorkspaceId, logger.NewChildLogger());
            }

            // Delete heartbeat, when Environment is deleted.
            return ContinuationResultHelpers.ReturnSucceeded();
        }

        private static CloudEnvironmentState GetTargetState(StartEnvironmentInputActionState actionState)
        {
            return actionState switch
            {
                StartEnvironmentInputActionState.CreateNew => CloudEnvironmentState.Provisioning,
                StartEnvironmentInputActionState.Export => CloudEnvironmentState.Exporting,
                StartEnvironmentInputActionState.Update => CloudEnvironmentState.Updating,
                _ => CloudEnvironmentState.Starting,
            };
        }

        private static async Task<bool> UpdateResourceInfoAsync(
            IStartEnvironmentContinuationPayloadV2 operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger,
            string operationBaseName)
        {
            return await logger.OperationScopeAsync(
                $"{operationBaseName}_update_resources_post_allocate",
                async (childLogger) =>
                {
                    var hasStorageResource = operationInput.StorageResource != default;
                    var hasOSDiskResource = operationInput.OSDiskResource != default;

                    var computeResource = operationInput.ComputeResource.BuildResourceRecord();
                    var osDiskResource = operationInput.OSDiskResource.BuildResourceRecord();
                    var storageResource = operationInput.StorageResource.BuildResourceRecord();

                    return await cloudEnvironmentRepository.UpdateRecordAsync(
                        operationInput.EnvironmentId,
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
                        logger,
                        operationBaseName);
                });
        }

        private static EnvironmentContinuationInputResource UpdateResourceStatus(
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

        private static bool ShouldEstablishWorkspaceConnection(this IStartEnvironmentContinuationPayloadV2 operationInput)
        {
            return operationInput.ActionState != StartEnvironmentInputActionState.Export &&
                operationInput.ActionState != StartEnvironmentInputActionState.Update;
        }

#pragma warning restore SA1600 // Elements should be documented
    }
}
