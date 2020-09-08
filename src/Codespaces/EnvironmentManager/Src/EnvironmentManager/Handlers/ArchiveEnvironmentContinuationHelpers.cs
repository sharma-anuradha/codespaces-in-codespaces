// <copyright file="ArchiveEnvironmentContinuationHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Archive env helpers.
    /// </summary>
    internal static class ArchiveEnvironmentContinuationHelpers
    {
#pragma warning disable SA1600 // Elements should be documented
        public static bool IsEnvironmentStateValidForArchive(
            this IArchiveEnvironmentContinuationPayload operationInput,
            CloudEnvironment environment,
            IDiagnosticsLogger logger)
        {
            var stillShutdown = environment.State == CloudEnvironmentState.Shutdown;
            var sameStateTarget = environment.LastStateUpdated == operationInput.LastStateUpdated;
            var isEnvironmentStateValidForArchive = stillShutdown && sameStateTarget;

            logger.FluentAddValue("ArchiveStillShutdown", stillShutdown)
                .FluentAddValue("ArchiveSameStateTarget", sameStateTarget)
                .FluentAddValue("ArchiveIsEnvironmentStateValidForArchive", isEnvironmentStateValidForArchive);

            return isEnvironmentStateValidForArchive;
        }

        public static async Task<ContinuationResult> RunAllocateStorageSnapshot(
            this IArchiveEnvironmentContinuationPayload operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Setup request object
            var allocateRequest = new AllocateRequestBody
            {
                Type = ResourceType.Snapshot,
                SkuName = record.Value.SkuName,
                Location = record.Value.Location,
            };
            allocateRequest.ExtendedProperties = new AllocateExtendedProperties
            {
                OSDiskResourceID = record.Value.OSDisk.ResourceId.ToString(),
            };

            return await RunAllocateRequest(
                operationInput,
                resourceBrokerHttpClient,
                record,
                allocateRequest,
                ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob,
                "InvalidSnapshotAllocate",
                logger);
        }

        public static async Task<ContinuationResult> RunAllocateStorageBlob(
            this IArchiveEnvironmentContinuationPayload operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Setup request object
            var allocateRequest = new AllocateRequestBody
            {
                Type = ResourceType.StorageArchive,
                SkuName = record.Value.SkuName,
                Location = record.Value.Location,
            };

            return await RunAllocateRequest(
                operationInput,
                resourceBrokerHttpClient,
                record,
                allocateRequest,
                ArchiveEnvironmentContinuationInputState.StartStorageBlob,
                "InvalidBlobStorageAllocate",
                logger);
        }

        public static async Task<ContinuationResult> RunAllocateRequest(
            this IArchiveEnvironmentContinuationPayload operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            AllocateRequestBody allocateRequest,
            ArchiveEnvironmentContinuationInputState archiveStatusIfSuccessful,
            string errorReason,
            IDiagnosticsLogger logger)
        {
            // Make request to allocate storage
            var allocateResponse = await resourceBrokerHttpClient.AllocateAsync(
                operationInput.EnvironmentId, allocateRequest, logger.NewChildLogger());
            if (allocateResponse != null)
            {
                // Map across details
                var archiveResource = new EnvironmentContinuationInputResource
                {
                    ResourceId = allocateResponse.ResourceId,
                    SkuName = allocateResponse.SkuName,
                    Location = allocateResponse.Location,
                    Created = allocateResponse.Created,
                    Type = allocateResponse.Type,
                };

                // Setup result
                operationInput.ArchiveResource = archiveResource;
                operationInput.ArchiveStatus = archiveStatusIfSuccessful;
                return ContinuationResultHelpers.ReturnInProgress(operationInput);
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = errorReason };
        }

        public static async Task<ContinuationResult> RunStartStorageBlob(
            this IArchiveEnvironmentContinuationPayload operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Setup request object
            var startIds = new List<StartRequestBody>()
            {
                new StartRequestBody { ResourceId = operationInput.ArchiveResource.ResourceId },
                new StartRequestBody { ResourceId = record.Value.Storage.ResourceId },
            };

            // Make request to start archive
            var successful = await resourceBrokerHttpClient.StartAsync(
                operationInput.EnvironmentId, StartRequestAction.StartArchive, startIds, logger.NewChildLogger());
            if (successful)
            {
                operationInput.ArchiveStatus = ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob;
                return ContinuationResultHelpers.ReturnInProgress(operationInput);
            }

            return ContinuationResultHelpers.ReturnFailed("InvalidBlobStorageStart");
        }

        public static async Task<ContinuationResult> RunCheckArchiveStatus(
            this IArchiveEnvironmentContinuationPayload operationInput,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Make request to check on start status
            var statusResponse = await resourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId, operationInput.ArchiveResource.ResourceId, logger.NewChildLogger());
            if (statusResponse.StartingStatus != null)
            {
                if (statusResponse.StartingStatus == OperationState.Succeeded)
                {
                    // Pass to next continuation
                    operationInput.ArchiveStatus = ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage;
                    return ContinuationResultHelpers.ReturnInProgress(operationInput);
                }
                else if (statusResponse.StartingStatus == OperationState.Initialized
                    || statusResponse.StartingStatus == OperationState.InProgress)
                {
                    return ContinuationResultHelpers.ReturnInProgress(operationInput, TimeSpan.FromSeconds(30));
                }
            }

            return ContinuationResultHelpers.ReturnFailed("InvalidBlobStorageStartStatus");
        }

        public static async Task<ContinuationResult> RunCleanupUnneededResources(
            this IArchiveEnvironmentContinuationPayload operationInput,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IEnvironmentStateManager environmentStateManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger,
            string operationName)
        {
            var resourceToDelete = operationInput.ArchiveResource.Type == ResourceType.Snapshot ? record.Value.OSDisk.ResourceId : record.Value.Storage.ResourceId;

            // Switch old fileshare for new blob
            var switchedStorage = await cloudEnvironmentRepository.UpdateRecordAsync(
                operationInput.EnvironmentId,
                record,
                async (environment, innerLogger) =>
                {
                    // Deal with case where the state has changed between retries
                    if (!IsEnvironmentStateValidForArchive(operationInput, record.Value, innerLogger))
                    {
                        return false;
                    }

                    var updatedRecord = new ResourceAllocationRecord
                    {
                        Created = operationInput.ArchiveResource.Created,
                        Location = operationInput.ArchiveResource.Location,
                        ResourceId = operationInput.ArchiveResource.ResourceId,
                        SkuName = operationInput.ArchiveResource.SkuName,
                        Type = operationInput.ArchiveResource.Type,
                    };

                    if (operationInput.ArchiveResource.Type == ResourceType.Snapshot)
                    {
                        // Remove osDisk reference and add OS disk snapshot
                        environment.OSDisk = null;
                        environment.OSDiskSnapshot = updatedRecord;
                    }
                    else
                    {
                        // Switch out old storage for archived storage
                        environment.Storage = updatedRecord;
                    }

                    // Update state to be archived
                    await environmentStateManager.SetEnvironmentStateAsync(
                        environment, CloudEnvironmentState.Archived, "ArchiveComplete", MessageCodes.EnvironmentArchived.ToString(), null, innerLogger);

                    return true;
                },
                logger,
                operationName);

            // Bail out if we couldn't update
            if (!switchedStorage)
            {
                return ContinuationResultHelpers.ReturnFailed("FailedStorageSwap");
            }

            // Make request to delete original storage
            var successful = await resourceBrokerHttpClient.DeleteAsync(operationInput.EnvironmentId, resourceToDelete, logger.NewChildLogger());
            if (successful)
            {
                return ContinuationResultHelpers.ReturnSucceeded();
            }

            return ContinuationResultHelpers.ReturnFailed("FailedStorageDelete");
        }
#pragma warning restore SA1600 // Elements should be documented
    }
}
