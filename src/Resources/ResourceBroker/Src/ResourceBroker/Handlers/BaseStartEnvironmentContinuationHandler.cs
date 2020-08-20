// <copyright file="BaseStartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environment.
    /// </summary>
    /// <typeparam name="TI">Type of the target input.</typeparam>
    public abstract class BaseStartEnvironmentContinuationHandler<TI>
        : BaseContinuationTaskMessageHandler<TI>
        where TI : BaseStartEnvironmentContinuationInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStartEnvironmentContinuationHandler{TI}"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="queueProvider">Queue provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        public BaseStartEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IQueueProvider queueProvider,
            IResourceStateManager resourceStateManager)
            : base(serviceProvider, resourceRepository, resourceStateManager)
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

        /// <summary>
        /// Builds the input required for the target continuation.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="compute">Referene to compute of target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Required target input.</returns>
        protected async Task<ContinuationInput> ConfigureBuildOperationInputAsync(TI input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            var computeOs = compute.Value.PoolReference.GetComputeOS();

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
            var didParseLocation = Enum.TryParse(compute.Value.Location, true, out AzureLocation azureLocation);
            if (!didParseLocation)
            {
                throw new NotSupportedException($"Provided location of '{compute.Value.Location}' is not supported.");
            }

            // Archive blob input setup
            if (input.ArchiveStorageResourceId != null)
            {
                await SetupArchiveStorageInfo(input, input.ArchiveStorageResourceId.Value, logger);
            }

            // Add target storage id
            input.EnvironmentVariables.Add("computeResourceId", input.ResourceId.ToString());

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

            // Pass user secrets to VirtualMachineProviderStartComputeInput only if environment is starting.
            return CreateStartComputeInput(input, compute, shareConnectionInfo, computeOs, azureLocation);
        }

        /// <summary>
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="input">Target operation input.</param>
        /// <param name="compute">Reference to target compute resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target operations continuation result.</returns>
        protected async Task<ContinuationResult> ConfigureRunOperationCoreAsync(TI input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            var queueComponent = compute.Value.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
            if (queueComponent == default)
            {
                return await ComputeProvider.StartComputeAsync((VirtualMachineProviderStartComputeInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                return await logger.OperationScopeAsync(
                    $"{LogBaseName}_start_compute_post_queue_message",
                    async (childLogger) =>
                    {
                        var startComputeInput = (VirtualMachineProviderStartComputeInput)input.OperationInput;
                        var queueMessage = GeneratePayload(startComputeInput);

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

        /// <summary>
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="input">Base start environment continuation input.</param>
        /// <param name="record">Resource record reference.</param>
        /// <param name="shareConnectionInfo">Share connection info.</param>
        /// <param name="computeOs">Compute OS. </param>
        /// <param name="azureLocation">Azure location.</param>
        /// <returns>Target operations continuation result.</returns>
        protected abstract VirtualMachineProviderStartComputeInput CreateStartComputeInput(TI input, ResourceRecordRef record, ShareConnectionInfo shareConnectionInfo, ComputeOS computeOs, AzureLocation azureLocation);

        /// <summary>
        /// Generate start payload.
        /// </summary>
        /// <param name="startComputeInput">Start compute input.</param>
        /// <returns>Queue Message.</returns>
        protected abstract QueueMessage GeneratePayload(VirtualMachineProviderStartComputeInput startComputeInput);

        private async Task<FileShareProviderAssignResult> StartStorageAsync(TI input, Guid storageId, ComputeOS computeOS, IDiagnosticsLogger logger)
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

        private async Task SetupArchiveStorageInfo(TI input, Guid archiveStorageResourceId, IDiagnosticsLogger logger)
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
