// <copyright file="BatchArchiveFileShareJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Batch Suspend File Share Job Provider.
    /// </summary>
    public class BatchArchiveFileShareJobProvider : BatchJobProvider<BatchArchiveFileShareJobInput>, IBatchArchiveFileShareJobProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchArchiveFileShareJobProvider"/> class.
        /// </summary>
        /// <param name="storageFileShareProviderHelper">Storage file share provider helper.</param>
        /// <param name="systemCatalog">System catalog.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public BatchArchiveFileShareJobProvider(
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            ISystemCatalog systemCatalog,
            IBatchClientFactory batchClientFactory,
            StorageProviderSettings storageProviderSettings)
            : base(storageFileShareProviderHelper, systemCatalog, batchClientFactory, storageProviderSettings)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "batch_archive_file_share_job_provider";

        /// <inheritdoc/>
        public Task<BatchTaskInfo> StartArchiveFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            string destBlobUriWithSas,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(destBlobUriWithSas, nameof(destBlobUriWithSas));

            var input = new BatchArchiveFileShareJobInput
            {
                DestBlobUriWithSas = destBlobUriWithSas,
            };

            return StartBatchTaskAsync(azureResourceInfo, input, logger);
        }

        /// <inheritdoc/>
        protected override CloudJob ConstructJob(
            BatchClient batchClient,
            string jobId,
            BatchArchiveFileShareJobInput taskInput)
        {
            var poolInformation = new PoolInformation { PoolId = StorageProviderSettings.WorkerBatchPoolId, };
            var job = batchClient.JobOperations.CreateJob(jobId, poolInformation);

            job.DisplayName = $"Archive{jobId}";

            return job;
        }

        /// <inheritdoc/>
        protected override CloudTask ConstructTask(
            string jobId,
            string taskId,
            CloudFileShare fileShare,
            BatchArchiveFileShareJobInput taskInput,
            IDiagnosticsLogger logger)
        {
            // The SaS tokens applies to the entire file share where all items are copied to.
            var destFileSas = fileShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
            {
                Permissions = SharedAccessFilePermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4),
            });

            var storageType = StorageType.Linux;

            var mountableFileName = StorageFileShareProviderHelper.GetStorageMountableFileName(storageType);
            var srcFile = fileShare.GetRootDirectoryReference().GetFileReference(mountableFileName);
            var srcFileUriWithSas = srcFile.Uri.AbsoluteUri + destFileSas;
            var taskCopyCommand = $"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{srcFileUriWithSas}' '{taskInput.DestBlobUriWithSas}'";

            logger.FluentAddValue($"SourceStorageFilePath-{storageType}", srcFile.Uri.ToString());

            // Define the task
            var taskCommandLine = $"/bin/bash -cxe \"printenv && {taskCopyCommand}\"";

            var task = new CloudTask(taskId, taskCommandLine)
            {
                Constraints = new TaskConstraints(maxTaskRetryCount: 3, maxWallClockTime: TimeSpan.FromMinutes(30), retentionTime: TimeSpan.FromDays(1)),
            };
            return task;
        }

        /// <inheritdoc/>
        protected override string GetBatchJobIdFromInput(BatchArchiveFileShareJobInput taskInput, int jobSuffix)
        {
            return $"{StorageProviderSettings.WorkerBatchPoolId}_Suspend_{jobSuffix}";
        }
    }
}
