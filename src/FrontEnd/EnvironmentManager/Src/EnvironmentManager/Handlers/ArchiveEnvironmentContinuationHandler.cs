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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

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
        /// <param name="environmentStateManager">Target environment manager.</param>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="resourceBrokerHttpClient">Target Resource Broker Http Client.</param>
        public ArchiveEnvironmentContinuationHandler(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient)
            : base(cloudEnvironmentRepository)
        {
            ResourceBrokerHttpClient = resourceBrokerHttpClient;
            EnvironmentStateManager = environmentStateManager;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => EnvironmentLoggingConstants.ContinuationTaskMessageHandlerArchive;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override EnvironmentOperation Operation => EnvironmentOperation.Archiving;

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
                    // Trigger blob allocate by calling allocate endpoint
                    return await RunAllocateStorageBlob(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.StartStorageBlob:
                    // Trigger blob copy by calling start endpoint
                    return await RunStartStorageBlob(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.CheckStartStorageBlob:
                    // Trigger blob copy check by calling start check endpoint
                    return await RunCheckStartStorageBlob(operationInput, record, logger);
                case ArchiveEnvironmentContinuationInputState.CleanupUnneededStorage:
                    // Trigger storage delete by calling delete endpoint
                    return await RunCleanupUnneededStorage(operationInput, record, logger);
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
            // If we didn't get as far as switching the blob, then we need to delete the blob
            if (record.Value.Storage != null
                && record.Value.Storage?.Type != ResourceType.StorageArchive
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

            // Make request to allocate storage
            var allocateResponse = await ResourceBrokerHttpClient.AllocateAsync(
                operationInput.EnvironmentId, allocateRequest, logger.NewChildLogger());
            if (allocateResponse != null)
            {
                // Map across details
                var blobResult = new ArchiveEnvironmentContinuationInputResource
                {
                    ResourceId = allocateResponse.ResourceId,
                    SkuName = allocateResponse.SkuName,
                    Location = allocateResponse.Location,
                    Created = allocateResponse.Created,
                    Type = allocateResponse.Type,
                };

                // Setup result
                operationInput.ArchiveResource = blobResult;
                operationInput.ArchiveStatus = ArchiveEnvironmentContinuationInputState.StartStorageBlob;
                return new ContinuationResult
                    {
                        NextInput = operationInput,
                        Status = OperationState.InProgress,
                    };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidBlobStorageAllocate" };
        }

        private async Task<ContinuationResult> RunStartStorageBlob(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Setup request object
            var blobId = new StartRequestBody { ResourceId = operationInput.ArchiveResource.ResourceId };
            var storageId = new StartRequestBody { ResourceId = record.Value.Storage.ResourceId };
            var startIds = new List<StartRequestBody> { blobId, storageId };

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

        private async Task<ContinuationResult> RunCheckStartStorageBlob(
            ArchiveEnvironmentContinuationInput operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            // Make request to check on start status
            var statusResponse = await ResourceBrokerHttpClient.StatusAsync(
                operationInput.EnvironmentId, operationInput.ArchiveResource.ResourceId, logger.NewChildLogger());
            if (statusResponse.StartingStatus != null)
            {
                if (statusResponse.StartingStatus == OperationState.Succeeded)
                {
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
                            RetryAfter = TimeSpan.FromSeconds(5),
                        };
                }
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "InvalidBlobStorageStartStatus" };
        }

        private async Task<ContinuationResult> RunCleanupUnneededStorage(
            ArchiveEnvironmentContinuationInput operationInput,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            var originalStorageId = record.Value.Storage.ResourceId;

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

                    // Switch out old storage for archived storage
                    environment.Storage = new ResourceAllocation
                    {
                        Created = operationInput.ArchiveResource.Created,
                        Location = operationInput.ArchiveResource.Location,
                        ResourceId = operationInput.ArchiveResource.ResourceId,
                        SkuName = operationInput.ArchiveResource.SkuName,
                        Type = operationInput.ArchiveResource.Type,
                    };

                    // Update state to be archived
                    await EnvironmentStateManager.SetEnvironmentStateAsync(
                        environment, CloudEnvironmentState.Archived, "ArchiveComplete", null, innerLogger);

                    return true;
                },
                logger);

            // Bail out if we couldn't update
            if (!switchedStorage)
            {
                return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedStorageSwap" };
            }

            // Make request to delete original storage
            var successful = await ResourceBrokerHttpClient.DeleteAsync(
                operationInput.EnvironmentId, originalStorageId, logger.NewChildLogger());
            if (successful)
            {
                return new ContinuationResult { Status = OperationState.Succeeded };
            }

            return new ContinuationResult { Status = OperationState.Failed, ErrorReason = "FailedStorageDelete" };
        }
    }
}
