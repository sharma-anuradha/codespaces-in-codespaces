// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environment.
    /// </summary>
    public class StartEnvironmentContinuationHandler
        : BaseContinuationTaskMessageHandler<StartEnvironmentContinuationInput>, IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobStartEnvironment";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        public StartEnvironmentContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IStorageFileShareProviderHelper storageFileShareProviderHelper)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            StorageFileShareProviderHelper = storageFileShareProviderHelper;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStartEnvironment;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.StartEnvironment;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IStorageFileShareProviderHelper StorageFileShareProviderHelper { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
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
                    storageResult.StorageFileName);
            }

            return new VirtualMachineProviderStartComputeInput(
                compute.Value.AzureResourceInfo,
                shareConnectionInfo,
                input.EnvironmentVariables,
                computeOs,
                azureLocation,
                compute.Value.SkuName,
                null);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(StartEnvironmentContinuationInput input, ResourceRecordRef compute, IDiagnosticsLogger logger)
        {
            return await ComputeProvider.StartComputeAsync((VirtualMachineProviderStartComputeInput)input.OperationInput, logger.NewChildLogger());
        }

        private async Task<FileShareProviderAssignResult> StartStorageAsync(StartEnvironmentContinuationInput input, Guid storageId, ComputeOS computeOS, IDiagnosticsLogger logger)
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

        private async Task SetupArchiveStorageInfo(StartEnvironmentContinuationInput input, Guid archiveStorageResourceId, IDiagnosticsLogger logger)
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
