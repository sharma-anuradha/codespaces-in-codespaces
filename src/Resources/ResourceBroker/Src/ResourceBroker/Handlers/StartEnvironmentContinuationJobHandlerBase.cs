// <copyright file="StartEnvironmentContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using QueueMessage = Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts.QueueMessage;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    public abstract class StartEnvironmentContinuationJobHandlerBase<TPayload, TResult>
        : ResourceContinuationJobHandlerBase<TPayload, EmptyContinuationState, TResult>
        where TPayload : StartEnvironmentContinuationPayloadBase
        where TResult : EntityContinuationResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStartEnvironmentContinuationHandler{TI}"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        protected StartEnvironmentContinuationJobHandlerBase(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(resourceRepository, resourceStateManager, jobQueueProducerFactory)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            StorageFileShareProviderHelper = storageFileShareProviderHelper;
            QueueProvider = queueProvider;
        }

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.StartEnvironment;

        /// <summary>
        /// Gets the compute provider.
        /// </summary>
        protected IComputeProvider ComputeProvider { get; }

        /// <summary>
        /// Gets the storage provider.
        /// </summary>
        protected IStorageProvider StorageProvider { get; }

        /// <summary>
        /// Gets the storage file share provider helper.
        /// </summary>
        protected IStorageFileShareProviderHelper StorageFileShareProviderHelper { get; }

        /// <summary>
        /// Gets the queue provider.
        /// </summary>
        protected IQueueProvider QueueProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<EmptyContinuationState, TResult>> ContinueAsync(TPayload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            if (payload.ComputeInput == null)
            {
                payload.ComputeInput = await CreateStartComputeInputAsync(payload, record, logger);
                return ReturnNextState();
            }

            var continuationResult = await StartComputeInputAsync(payload.ComputeInput, record, logger);
            return ToContinuationInfo(continuationResult, payload);
        }

        /// <summary>
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="input">Base start environment continuation input.</param>
        /// <param name="record">Resource record reference.</param>
        /// <param name="shareConnectionInfo">Share connection info.</param>
        /// <param name="computeOs">Compute OS. </param>
        /// <param name="azureLocation">Azure location.</param>
        /// <returns>Target operations continuation result.</returns>
        protected abstract VirtualMachineProviderStartComputeInput CreateStartComputeInput(TPayload input, IEntityRecordRef<ResourceRecord> record, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation);

        /// <summary>
        /// Generate start payload.
        /// </summary>
        /// <param name="startComputeInput">Start compute input.</param>
        /// <returns>Queue Message.</returns>
        protected abstract QueueMessage GeneratePayload(VirtualMachineProviderStartComputeInput startComputeInput);

        protected virtual async Task<VirtualMachineProviderStartComputeInput> CreateStartComputeInputAsync(TPayload input, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger)
        {
            var computeOs = record.Value.PoolReference.GetComputeOS();

            // Start storage
            FileShareProviderAssignResult storageResult = null;
            if (input.StorageResourceId != null)
            {
                storageResult = await StartStorageAsync(input, input.StorageResourceId.Value, computeOs, logger);
                if (storageResult.Status != OperationState.Succeeded)
                {
                    return null;
                }
            }

            // Parse location
            var didParseLocation = Enum.TryParse(record.Value.Location, true, out AzureLocation azureLocation);
            if (!didParseLocation)
            {
                throw new NotSupportedException($"Provided location of '{record.Value.Location}' is not supported.");
            }

            // Archive blob input setup
            if (input.ArchiveStorageResourceId != null)
            {
                await SetupArchiveStorageInfo(input, input.ArchiveStorageResourceId.Value, logger);
            }

            // Add target storage id
            input.EnvironmentVariables.Add("computeResourceId", input.EntityId.ToString());

            if (input.StorageResourceId != null)
            {
                input.EnvironmentVariables.Add("storageResourceId", input.StorageResourceId.Value.ToString());
            }

            ShareConnectionInfo shareConnectionInfo = null;
            if (input.StorageResourceId != null)
            {
                shareConnectionInfo = new ShareConnectionInfo(
                    storageResult.StorageAccountName,
                    storageResult.StorageAccountKey,
                    storageResult.StorageShareName,
                    storageResult.StorageFileName,
                    storageResult.StorageFileServiceHost);
            }

            return CreateStartComputeInput(input, record, shareConnectionInfo, computeOs, azureLocation);
        }

        private async Task<ContinuationResult> StartComputeInputAsync(VirtualMachineProviderStartComputeInput input, IEntityRecordRef<ResourceRecord> compute, IDiagnosticsLogger logger)
        {
            var queueComponent = compute.Value.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
            if (queueComponent == default)
            {
                return await ComputeProvider.StartComputeAsync(input, logger.NewChildLogger());
            }
            else
            {
                return await logger.OperationScopeAsync(
                    $"{LogBaseName}_start_compute_post_queue_message",
                    async (childLogger) =>
                    {
                        var queueMessage = GeneratePayload(input);

                        await QueueProvider.PushMessageAsync(
                            queueComponent.AzureResourceInfo,
                            queueMessage,
                            childLogger.NewChildLogger());

                        return new VirtualMachineProviderStartComputeResult()
                        {
                            Status = OperationState.Succeeded,
                        };
                    });
            }
        }

        private async Task<FileShareProviderAssignResult> StartStorageAsync(TPayload input, Guid storageId, ComputeOS computeOS, IDiagnosticsLogger logger)
        {
            // Fetch storage reference
            var fileReference = await FetchReferenceAsync(storageId, logger);

            // Update storage to be inprogress
            await UpdateRecordStatusAsync(input, fileReference, OperationState.Initialized, "PreAssignStorage", logger);

            // Get file share connection info for target share
            var storageType = computeOS == ComputeOS.Windows ? StorageType.Windows : StorageType.Linux;
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = fileReference.Value.AzureResourceInfo,
                StorageType = storageType,
            };
            var storageContinuationResult = await StorageProvider.StartAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await UpdateRecordStatusAsync(input, fileReference, storageContinuationResult.Status, "PostAssignStorage", logger);

            // If archive is present, setup file sas token
            if (input.ArchiveStorageResourceId != null)
            {
                // Get storage file share details
                var storageFile = await StorageFileShareProviderHelper.FetchStorageFileShareSasTokenAsync(
                    fileReference.Value.AzureResourceInfo,
                    storageContinuationResult.StorageAccountKey,
                    storageType,
                    SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                    "temp",
                    logger.NewChildLogger());

                input.EnvironmentVariables.Add("storageAccountSasToken", storageFile.Token);
                input.EnvironmentVariables.Add("storageFileNameTemp", storageFile.FileName);
            }

            return storageContinuationResult;
        }

        private async Task SetupArchiveStorageInfo(TPayload input, Guid archiveStorageResourceId, IDiagnosticsLogger logger)
        {
            // Fetch blob reference
            var archiveReference = await FetchReferenceAsync(archiveStorageResourceId, logger);
            var archiveShareRecordDetails = archiveReference.Value.GetStorageDetails();
            var archiveAzureInfo = archiveReference.Value.AzureResourceInfo;

            // Execute flow for tarrget Strategy
            if (archiveShareRecordDetails.ArchiveStorageStrategy == ResourceArchiveStrategy.BlobStorage)
            {
                // Get archive blob details
                var archiveBlob = await StorageFileShareProviderHelper.FetchBlobSasTokenAsync(
                    archiveAzureInfo,
                    null,
                    archiveShareRecordDetails.ArchiveStorageBlobContainerName,
                    archiveShareRecordDetails.ArchiveStorageBlobName,
                    SharedAccessBlobPermissions.Read,
                    logger.NewChildLogger());

                // Setup extra environment vars which describe archive blob
                input.EnvironmentVariables.Add("storageArchiveResourceId", input.ArchiveStorageResourceId.Value.ToString());
                input.EnvironmentVariables.Add("storageArchiveStrategy", archiveShareRecordDetails.ArchiveStorageStrategy.ToString());
                input.EnvironmentVariables.Add("storageArchiveBlobTargetSizeGb", archiveShareRecordDetails.ArchiveStorageSourceSizeInGb.ToString());
                input.EnvironmentVariables.Add("storageArchiveBlobAccountName", archiveAzureInfo.Name);
                input.EnvironmentVariables.Add("storageArchiveBlobContainerName", archiveBlob.BlobContainerName);
                input.EnvironmentVariables.Add("storageArchiveBlobFileName", archiveBlob.BlobName);
                input.EnvironmentVariables.Add("storageArchiveBlobFileSasToken", archiveBlob.Token);
            }
            else
            {
                throw new NotSupportedException("Targetted archive strategy is not supported.");
            }
        }
    }
}
