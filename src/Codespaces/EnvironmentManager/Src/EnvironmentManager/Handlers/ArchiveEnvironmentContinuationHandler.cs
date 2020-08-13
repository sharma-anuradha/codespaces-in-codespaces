// <copyright file="ArchiveEnvironmentContinuationHandler.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Archive Environment Continuation Handler.
    /// </summary>
    public class ArchiveEnvironmentContinuationHandler
        : BaseContinuationTaskMessageHandler<ArchiveEnvironmentContinuationInput>, IArchiveEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobArchiveEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="environmentManagerSettings">Environment manager settings.</param>
        /// <param name="environmentStateManager">Target environment manager.</param>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        public ArchiveEnvironmentContinuationHandler(
            EnvironmentManagerSettings environmentManagerSettings,
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient)
            : base(cloudEnvironmentRepository)
        {
            EnvironmentManagerSettings = environmentManagerSettings;
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerArchive;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Archiving;

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerHttpClient { get; }

        /// <inheritdoc/>
        protected override TransitionState FetchOperationTransition(
            ArchiveEnvironmentContinuationInput input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            return record.Value.Transitions.Archiving;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("EnvironmentId", operationInput.EnvironmentId)
                .FluentAddValue("ArchiveSubStatus", operationInput.ArchiveStatus)
                .FluentAddValue("ArchiveLastStateUpdated", operationInput.LastStateUpdated)
                .FluentAddValue("ArchiveReason", operationInput.Reason)
                .FluentAddValue("ArchiveResourceId", operationInput?.ArchiveResource?.ResourceId);

            // If the blob is no longer shutdown, then we should fail and cleanup
            if (!IsEnvironmentStateValidForArchive(operationInput, record.Value, logger))
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "StateNoLongerShutdown" };
            }

            // Run operation
            switch (operationInput.ArchiveStatus)
            {
                case ArchiveEnvironmentContinuationInputState.AllocateStorageBlob:
                    // Trigger resource allocation by calling allocate endpoint
                    if (record.Value.OSDisk != default)
                    {
                        // Only archive OS disks if feature is enabled
                        if (await EnvironmentManagerSettings.EnvironmentOSDiskArchiveEnabled(logger))
                        {
                            // Trigger snapshot allocation
                            return await RunAllocateStorageSnapshot(operationInput, record, logger);
                        }

                        return new ContinuationResult { Status = OperationState.Succeeded };
                    }

                    return await RunAllocateStorageBlob(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.StartStorageBlob:
                    // Trigger blob copy by calling start endpoint
                    return await RunStartStorageBlob(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob:
                    // Trigger blob copy check by calling start check endpoint
                    return await RunCheckArchiveStatus(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage:
                    // Trigger storage delete by calling delete endpoint
                    return await RunCleanupUnneededResources(operationInput, record, logger);
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidArchiveState" };
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
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // If we didn't get as far as switching the record, then we need to delete the allocated resource
            if (((record.Value.Storage != null
                && record.Value.Storage?.Type != ResourceType.StorageArchive)
                || record.Value.OSDisk != null)
                && operationInput.ArchiveResource != null)
            {
                // Make sure we update the archive state to cleanup
                await UpdateRecordAsync(
                    operationInput,
                    record,
                    (environment, innerLogger) =>
                    {
                        return Task.FromResult(environment.Transitions.Archiving.ResetStatus(false));
                    },
                    logger);

                // Trigger delete of resource that we tried to create
                var successful = await ResourceBrokerHttpClient.DeleteAsync(
                    operationInput.EnvironmentId, operationInput.ArchiveResource.ResourceId, logger.NewChildLogger());

                return new ContinuationResult
                {
                    Status = successful ? OperationState.Succeeded : OperationState.Failed,
                };
            }

            return await base.FailOperationCleanupCoreAsync(operationInput, record, trigger, logger);
        }

        private bool IsEnvironmentStateValidForArchive(
            ArchiveEnvironmentContinuationInput operationInput,
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

        private async Task<ContinuationResult> RunAllocateStorageSnapshot(
            ArchiveEnvironmentContinuationInput operationInput,
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
                record,
                allocateRequest,
                ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob,
                "InvalidSnapshotAllocate",
                logger);
        }

        private async Task<ContinuationResult> RunAllocateStorageBlob(
            ArchiveEnvironmentContinuationInput operationInput,
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
                record,
                allocateRequest,
                ArchiveEnvironmentContinuationInputState.StartStorageBlob,
                "InvalidBlobStorageAllocate",
                logger);
        }

        private async Task<ContinuationResult> RunAllocateRequest(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            AllocateRequestBody allocateRequest,
            ArchiveEnvironmentContinuationInputState archiveStatusIfSuccessful,
            string errorReason,
            IDiagnosticsLogger logger)
        {
            // Make request to allocate storage
            var allocateResponse = await ResourceBrokerHttpClient.AllocateAsync(
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
                return new ContinuationResult
                {
                    NextInput = operationInput,
                    Status = OperationState.InProgress,
                };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = errorReason };
        }

        private async Task<ContinuationResult> RunStartStorageBlob(
            ArchiveEnvironmentContinuationInput operationInput,
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
            var successful = await ResourceBrokerHttpClient.StartAsync(
                operationInput.EnvironmentId, StartRequestAction.StartArchive, startIds, logger.NewChildLogger());
            if (successful)
            {
                operationInput.ArchiveStatus = ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob;
                return new ContinuationResult
                {
                    NextInput = operationInput,
                    Status = OperationState.InProgress,
                };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidBlobStorageStart" };
        }

        private async Task<ContinuationResult> RunCheckArchiveStatus(
            ArchiveEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            // Make request to check on start status
            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId, operationInput.ArchiveResource.ResourceId, logger.NewChildLogger());
            if (statusResponse.StartingStatus != null)
            {
                if (statusResponse.StartingStatus == OperationState.Succeeded)
                {
                    // Pass to next continuation
                    operationInput.ArchiveStatus = ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage;
                    return new ContinuationResult
                    {
                        NextInput = operationInput,
                        Status = OperationState.InProgress,
                    };
                }
                else if (statusResponse.StartingStatus == OperationState.Initialized
                    || statusResponse.StartingStatus == OperationState.InProgress)
                {
                    return new ContinuationResult
                        {
                            NextInput = operationInput,
                            Status = OperationState.InProgress,
                            RetryAfter = TimeSpan.FromSeconds(30),
                        };
                }
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidBlobStorageStartStatus" };
        }

        private async Task<ContinuationResult> RunCleanupUnneededResources(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var resourceToDelete = operationInput.ArchiveResource.Type == ResourceType.Snapshot ? record.Value.OSDisk.ResourceId : record.Value.Storage.ResourceId;

            // Switch old fileshare for new blob
            var switchedStorage = await UpdateRecordAsync(
                operationInput,
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
                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        environment, CloudEnvironmentState.Archived, "ArchiveComplete", MessageCodes.EnvironmentArchived.ToString(), null, innerLogger);

                    return true;
                },
                logger);

            // Bail out if we couldn't update
            if (!switchedStorage)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedStorageSwap" };
            }

            // Make request to delete original storage
            var successful = await ResourceBrokerHttpClient.DeleteAsync(operationInput.EnvironmentId, resourceToDelete, logger.NewChildLogger());
            if (successful)
            {
                return new ContinuationResult { Status = OperationState.Succeeded };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedStorageDelete" };
        }
    }
}
