// <copyright file="StartArchiveContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation job handler that manages starting of archive.
    /// </summary>
    public class StartArchiveContinuationJobHandler
        : ResourceContinuationJobHandlerBase<StartArchiveContinuationJobHandler.Payload, EmptyContinuationState, StartArchiveContinuationJobHandler.StartArchiveContinuationResult>
    {
        /// <summary>
        /// Gets default queue id name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-start-archive";

        /// <summary>
        /// Initializes a new instance of the <see cref="StartArchiveContinuationJobHandler"/> class.
        /// </summary>
        /// <param name="archiveStorageProvider">Archive Storage Provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="storageFileShareProviderHelper">Storage File Share Provider Helper.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        public StartArchiveContinuationJobHandler(
            IArchiveStorageProvider archiveStorageProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(resourceRepository, resourceStateManager, jobQueueProducerFactory)
        {
            ArchiveStorageProvider = archiveStorageProvider;
            StorageProvider = storageProvider;
            StorageFileShareProviderHelper = storageFileShareProviderHelper;
        }

        public override string QueueId => DefaultQueueId;

        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerStartArchive;

        protected override ResourceOperation Operation => ResourceOperation.StartArchiving;

        private IArchiveStorageProvider ArchiveStorageProvider { get; set; }

        private IStorageProvider StorageProvider { get; }

        private IStorageFileShareProviderHelper StorageFileShareProviderHelper { get; }

        protected override async Task<ContinuationJobResult<EmptyContinuationState, StartArchiveContinuationResult>> ContinueAsync(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var result = (ContinuationResult)null;

            // Build result
            if (payload.ArchiveInput == null)
            {
                payload.ArchiveInput = await BuildArchiveInputAsync(payload, record, logger);
            }

            result = await RunArchiveOperationAsync(payload, (ResourceRecordRef)record, logger);

            // Keep track of the returned next input
            if (result.NextInput != null)
            {
                payload.ArchiveInput = (FileShareProviderArchiveInput)result.NextInput;
            }

            return ToContinuationInfo(result, payload);
        }

        private async Task<FileShareProviderArchiveInput> BuildArchiveInputAsync(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger)
        {
            // Get file share details
            var fileShareReference = await FetchReferenceAsync(payload.FileShareResourceId, logger);
            var fileShareRecordDetails = fileShareReference.Value.GetStorageDetails();

            // Get archive blob details
            var archiveStorageInfo = await ArchiveStorageProvider.GetArchiveStorageAccountAsync(
                fileShareRecordDetails.Location, fileShareRecordDetails.SizeInGB, logger.NewChildLogger());
            var blobRecordDetails = record.Value.GetStorageDetails();

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
                payload.EnvironmentId.ToString(),
                archiveStorageInfo.StorageAccountKey,
                SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                logger.NewChildLogger());

            // Update archive info on record
            await UpdateRecordAsync(
                payload,
                record,
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

                    return Task.FromResult(true);
                },
                logger.NewChildLogger());

            return new FileShareProviderArchiveInput
                {
                    SrcAzureResourceInfo = fileShareReference.Value.AzureResourceInfo,
                    SrcFileShareUriWithSas = fileShare.Token,
                    DestBlobUriWithSas = archiveBlob.Token,
                };
        }

        private async Task<ContinuationResult> RunArchiveOperationAsync(Payload payload, ResourceRecordRef blobReference, IDiagnosticsLogger logger)
        {
            var result = await StorageProvider.ArchiveAsync(payload.ArchiveInput, logger.NewChildLogger());

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
                    payload,
                    blobReference,
                    (blobRecord, innerLogger) =>
                    {
                        // Save extra blob info to the storage details
                        blobRecordDetails = blobRecord.GetStorageDetails();
                        blobRecordDetails.ArchiveStorageBlobStoredSizeInGb = totalGb;

                        return Task.FromResult(true);
                    },
                    logger.NewChildLogger());
            }

            return result;
        }

        /// <summary>
        /// This is the payload that mimic the 'StartArchiveContinuationInput' type
        /// </summary>
        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<EmptyContinuationState>
        {
            /// <summary>
            /// Gets or sets the environment id.
            /// </summary>
            public Guid EnvironmentId { get; set; }

            /// <summary>
            /// Gets or sets the source storage resource id.
            /// </summary>
            public Guid FileShareResourceId { get; set; }

            /// <summary>
            /// Gets or sets the ArchiveInput
            /// </summary>
            public FileShareProviderArchiveInput ArchiveInput { get; set; }
        }

        public class StartArchiveContinuationResult : EntityContinuationResult
        {
        }
    }
}
