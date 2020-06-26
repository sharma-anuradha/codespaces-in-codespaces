// <copyright file="StartArchiveContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of archive.
    /// </summary>
    public class StartArchiveContinuationHandler
        : BaseContinuationTaskMessageHandler<StartArchiveContinuationInput>, IStartArchiveContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobStartArchive";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartArchiveContinuationHandler"/> class.
        /// </summary>
        /// <param name="archiveStorageProvider">Archive Storage Provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        public StartArchiveContinuationHandler(
            IArchiveStorageProvider archiveStorageProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IStorageFileShareProviderHelper storageFileShareProviderHelper)
            : base(serviceProvider, resourceRepository)
        {
            ArchiveStorageProvider = archiveStorageProvider;
            StorageProvider = storageProvider;
            StorageFileShareProviderHelper = storageFileShareProviderHelper;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStartArchive;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.StartArchiving;

        private IArchiveStorageProvider ArchiveStorageProvider { get; set; }

        private IStorageProvider StorageProvider { get; }

        private IStorageFileShareProviderHelper StorageFileShareProviderHelper { get; }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(StartArchiveContinuationInput input, ResourceRecordRef blobReference, IDiagnosticsLogger logger)
        {
            // Get file share details
            var fileShareReference = await FetchReferenceAsync(input.FileShareResourceId, logger);
            var fileShareRecordDetails = fileShareReference.Value.GetStorageDetails();

            // Get archive blob details
            var archiveStorageInfo = await ArchiveStorageProvider.GetArchiveStorageAccountAsync(
                fileShareRecordDetails.Location, fileShareRecordDetails.SizeInGB, logger.NewChildLogger());
            var blobRecordDetails = blobReference.Value.GetStorageDetails();

            logger.FluentAddValue("ArchiveStorageBlobTargetSizeGb", fileShareRecordDetails.SizeInGB);

            // Validate that source compute os is present
            var blobSourceComputeOS = blobRecordDetails.SourceComputeOS;
            if (blobSourceComputeOS == null)
            {
                throw new ArgumentNullException("Source compute os wasn't provided to archive record");
            }

            // Fetch file share storage details
            var fileShare = await StorageFileShareProviderHelper.FetchStorageFileShareSasTokenAsync(
                fileShareReference.Value.AzureResourceInfo,
                null,
                blobSourceComputeOS.Value == ComputeOS.Windows ? StorageType.Windows : StorageType.Linux,
                SharedAccessFilePermissions.Read,
                filePrefix: null,
                logger: logger.NewChildLogger());

            // Fetch blob storage details
            var archiveBlob = await StorageFileShareProviderHelper.FetchArchiveBlobSasTokenAsync(
                archiveStorageInfo.AzureResourceInfo,
                input.EnvironmentId.ToString(),
                archiveStorageInfo.StorageAccountKey,
                SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                logger.NewChildLogger());

            // Update archive info on record
            await UpdateRecordAsync(
                input,
                blobReference,
                (blobRecord, innerLogger) =>
                {
                    // Update key Azure resource info
                    blobRecord.AzureResourceInfo = archiveStorageInfo.AzureResourceInfo;

                    // Save extra blob info to the storage details
                    blobRecordDetails = blobRecord.GetStorageDetails();
                    blobRecordDetails.ArchiveStorageStrategy = ResourceArchiveStrategy.BlobStorage;
                    blobRecordDetails.ArchiveStorageBlobContainerName = archiveBlob.BlobContainerName;
                    blobRecordDetails.ArchiveStorageBlobName = archiveBlob.BlobName;
                    blobRecordDetails.ArchiveStorageSourceSizeInGb = fileShareRecordDetails.SizeInGB;
                    blobRecordDetails.ArchiveStorageSourceResourceId = fileShareReference.Value.Id;
                    blobRecordDetails.ArchiveStorageSourceStorageAccountName = fileShareReference.Value.AzureResourceInfo.Name;
                    blobRecordDetails.ArchiveStorageSourceSkuName = fileShareReference.Value.SkuName;
                    blobRecordDetails.ArchiveStorageSourceFileName = fileShare.FileName;
                    blobRecordDetails.ArchiveStorageSourceFileShareName = fileShare.FileShareName;

                    return true;
                },
                logger.NewChildLogger());

            // Build result
            return new FileShareProviderArchiveInput
                {
                    SrcAzureResourceInfo = fileShareReference.Value.AzureResourceInfo,
                    SrcFileShareUriWithSas = fileShare.Token,
                    DestBlobUriWithSas = archiveBlob.Token,
                };
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(StartArchiveContinuationInput input, ResourceRecordRef blobReference, IDiagnosticsLogger logger)
        {
            var result = await StorageProvider.ArchiveAsync((FileShareProviderArchiveInput)input.OperationInput, logger.NewChildLogger());

            // Update the total size of the shunk drive
            if (result.Status == OperationState.Succeeded)
            {
                // Get current reference
                var blobRecordDetails = blobReference.Value.GetStorageDetails();

                // Setup blob container
                var blobContainerName = blobRecordDetails.ArchiveStorageBlobContainerName;
                var blobName = blobRecordDetails.ArchiveStorageBlobName;
                var cloudBlobReference = await StorageFileShareProviderHelper.FetchBlobAsync(
                    blobReference.Value.AzureResourceInfo, null, blobContainerName, blobName, logger.NewChildLogger());
                var blob = cloudBlobReference.Blob;

                // Trigger fetch of attributes so we can geet the length
                blob.FetchAttributes();

                // Convert to gb
                var totalBytes = blob.Properties.Length;
                var totalGb = totalBytes / 1024.0 / 1024.0 / 1024.0;

                logger.FluentAddValue("ArchiveStorageBlobStoredSizeInGb", totalGb)
                    .FluentAddValue("ArchiveStorageSourceSizeInGb", blobRecordDetails.ArchiveStorageSourceSizeInGb);

                // Conduct the actual update
                await UpdateRecordAsync(
                    input,
                    blobReference,
                    (blobRecord, innerLogger) =>
                    {
                        // Save extra blob info to the storage details
                        blobRecordDetails = blobRecord.GetStorageDetails();
                        blobRecordDetails.ArchiveStorageBlobStoredSizeInGb = totalGb;

                        return true;
                    },
                    logger.NewChildLogger());
            }

            return result;
        }
    }
}
