// <copyright file="BatchPrepareFileShareJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Batch Prepare File Share Job Provider.
    /// </summary>
    public class BatchPrepareFileShareJobProvider : BatchJobProvider<BatchPrepareFileShareJobInput>, IBatchPrepareFileShareJobProvider
    {
        private const string TaskDisplayName = TaskConstants.PrepareTaskDisplayName;
        private readonly string batchJobMetadataKey = "SourceBlobFilename";

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchPrepareFileShareJobProvider"/> class.
        /// </summary>
        /// <param name="storageFileShareProviderHelper">Storage file share provider helper.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="azureClientFactory">The azure client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public BatchPrepareFileShareJobProvider(
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IBatchClientFactory batchClientFactory,
            IAzureClientFactory azureClientFactory,
            StorageProviderSettings storageProviderSettings)
            : base(storageFileShareProviderHelper, batchClientFactory, azureClientFactory, storageProviderSettings)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "batch_prepare_file_share_job_provider";

        /// <inheritdoc/>
        public Task<BatchTaskInfo> StartPrepareFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            IEnumerable<StorageCopyItem> sourceCopyItems,
            int storageSizeInGb,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(sourceCopyItems, nameof(sourceCopyItems));

            var input = new BatchPrepareFileShareJobInput
            {
                SourceCopyItems = sourceCopyItems,
                StorageSizeInGb = storageSizeInGb,
            };

            return StartBatchTaskAsync(azureResourceInfo, input, logger);
        }

        /// <inheritdoc/>
        protected override CloudJob ConstructJob(
            BatchClient batchClient,
            string jobId,
            BatchPrepareFileShareJobInput taskInput)
        {
            var jobMetadata = new List<MetadataItem>();
            var jobWorkingDir = GetJobWorkingDirectory(jobId);
            var poolInformation = new PoolInformation { PoolId = StorageProviderSettings.WorkerBatchPoolId };

            var jobPrepareCommandLines = new List<string>();
            foreach (var copyItem in taskInput.SourceCopyItems)
            {
                jobPrepareCommandLines.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{copyItem.SrcBlobUrl}' {jobWorkingDir}{copyItem.SrcBlobFileName}");
                if (copyItem.StorageType == StorageType.Linux)
                {
                    jobPrepareCommandLines.Add($"resize2fs -fp {jobWorkingDir}{copyItem.SrcBlobFileName} {taskInput.StorageSizeInGb}G");
                }

                jobMetadata.Add(new MetadataItem($"{batchJobMetadataKey}-{copyItem.StorageType}", copyItem.SrcBlobFileName));
            }

            var job = batchClient.JobOperations.CreateJob(jobId, poolInformation);
            job.DisplayName = TaskDisplayName;
            job.JobPreparationTask = new JobPreparationTask
            {
                CommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", jobPrepareCommandLines)}\"",
                Constraints = new TaskConstraints
                {
                    RetentionTime = TimeSpan.FromDays(7),
                    MaxWallClockTime = TimeSpan.FromMinutes(10),
                    MaxTaskRetryCount = 3,
                },
                RerunOnComputeNodeRebootAfterSuccess = true,
                WaitForSuccess = true,
            };
            job.JobReleaseTask = new JobReleaseTask
            {
                CommandLine = $"/bin/bash -cxe \"printenv && rm -rf {jobWorkingDir}\"",
                RetentionTime = TimeSpan.FromDays(7),
                MaxWallClockTime = TimeSpan.FromMinutes(10),
            };
            job.Metadata = jobMetadata;
            job.Constraints = new JobConstraints(maxWallClockTime: TimeSpan.FromDays(90));
            job.Priority = 200;

            return job;
        }

        /// <inheritdoc/>
        protected override CloudTask ConstructTask(
            string jobId,
            string taskId,
            CloudFileShare fileShare,
            BatchPrepareFileShareJobInput taskInput,
            IDiagnosticsLogger logger)
        {
            var jobWorkingDir = GetJobWorkingDirectory(jobId);

            // The SaS tokens applies to the entire file share where all items are copied to.
            var destFileSas = fileShare.GetSharedAccessSignature(new SharedAccessFilePolicy()
            {
                Permissions = SharedAccessFilePermissions.Read | SharedAccessFilePermissions.Write,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4),
            });

            var taskCopyCommands = new List<string>();
            foreach (var copyItem in taskInput.SourceCopyItems)
            {
                var mountableFileName = StorageFileShareProviderHelper.GetStorageMountableFileName(copyItem.StorageType);
                var destFile = fileShare.GetRootDirectoryReference().GetFileReference(mountableFileName);
                var destHostName = destFile.Uri.Host;
                var destFileUriWithSas = destFile.Uri.AbsoluteUri + destFileSas;
                taskCopyCommands.Add($"host -W 10 '{destHostName}'");
                taskCopyCommands.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp {jobWorkingDir}{copyItem.SrcBlobFileName} '{destFileUriWithSas}'");

                logger.FluentAddValue($"DestinationStorageFilePath-{copyItem.StorageType}", destFile.Uri.ToString());
            }

            // Define the task
            var taskCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", taskCopyCommands)}\"";

            var task = new CloudTask(taskId, taskCommandLine)
            {
                Constraints = new TaskConstraints(maxTaskRetryCount: 0, maxWallClockTime: TimeSpan.FromMinutes(30), retentionTime: TimeSpan.FromDays(7)),
            };
            return task;
        }

        /// <inheritdoc/>
        protected override string GetBatchJobIdFromInput(BatchPrepareFileShareJobInput taskInput, int jobSuffix)
        {
            return $"{(StorageProviderSettings.WorkerBatchPoolId + taskInput.StorageSizeInGb + JoinCopyItemFileNames(taskInput.SourceCopyItems)).GetDeterministicHashCode()}_{jobSuffix}";
        }

        private string JoinCopyItemFileNames(IEnumerable<StorageCopyItem> copyItems) => string.Join(",", copyItems.Select(s => $"{s.StorageType.ToString()}={s.SrcBlobFileName}"));
    }
}
