// <copyright file="BatchArchiveFileShareJobProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Batch Suspend File Share Job Provider.
    /// </summary>
    public class BatchArchiveFileShareJobProvider : BatchJobProvider<BatchArchiveFileShareJobInput>, IBatchArchiveFileShareJobProvider
    {
        private const int TaskTimeoutMin = TaskConstants.ArchiveTaskTimeoutMin;

        private const string TaskDisplayName = TaskConstants.ArchiveTaskDisplayName;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchArchiveFileShareJobProvider"/> class.
        /// </summary>
        /// <param name="storageFileShareProviderHelper">Storage file share provider helper.</param>
        /// <param name="batchClientFactory">Batch client factory.</param>
        /// <param name="azureClientFactory">The azure client factory.</param>
        /// <param name="storageProviderSettings">The storage provider settings.</param>
        public BatchArchiveFileShareJobProvider(
            IStorageFileShareProviderHelper storageFileShareProviderHelper,
            IBatchClientFactory batchClientFactory,
            IAzureClientFactory azureClientFactory,
            StorageProviderSettings storageProviderSettings)
            : base(storageFileShareProviderHelper, batchClientFactory, azureClientFactory, storageProviderSettings)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "batch_archive_file_share_job_provider";

        /// <inheritdoc/>
        public Task<BatchTaskInfo> StartArchiveFileShareAsync(
            AzureResourceInfo azureResourceInfo,
            string srcFileShareUriWithSas,
            string destBlobUriWithSas,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(destBlobUriWithSas, nameof(destBlobUriWithSas));

            var input = new BatchArchiveFileShareJobInput
            {
                SrcFileShareUriWithSas = srcFileShareUriWithSas,
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

            var jobPrepareCommandLines = new List<string>();

            // Make sure the target directory exists
            var localSrc = "/datadrive/archives/";
            jobPrepareCommandLines.Add($"mkdir -p {localSrc}");

            // Set permissions
            jobPrepareCommandLines.Add($"chmod 777 {localSrc}");

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
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),
            };
            job.Priority = 100;

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
            var localSrc = "/datadrive/archives/";
            var localTargetSrc = $"{localSrc}{taskId}_{DateTime.UtcNow.Ticks}";

            var taskCopyCommand = new List<string>();

            // Print out the list of file so far
            taskCopyCommand.Add("echo ----- DATA DRIVE DETAILS -----");
            taskCopyCommand.Add($"df -H /datadrive");

            // Print out the list of file so far
            taskCopyCommand.Add("echo ----- FOUND FILES -----");
            taskCopyCommand.Add($"ls -la {localSrc}");

            // Clear out any old images just incase something got missed in cleanup
            taskCopyCommand.Add("echo ----- FIND OLD FILES -----");
            taskCopyCommand.Add($"find {localSrc} -maxdepth 1 -type f -mmin +{TaskTimeoutMin}");

            // Clear out any old images just incase something got missed in cleanup
            taskCopyCommand.Add("echo ----- DELETE OLD FILES -----");
            taskCopyCommand.Add($"find {localSrc} -maxdepth 1 -type f -mmin +{TaskTimeoutMin} -delete || true");

            // Copy target share to local disk
            taskCopyCommand.Add("echo ----- COPY DOWN BLOB -----");
            taskCopyCommand.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{taskInput.SrcFileShareUriWithSas}' '{localTargetSrc}' --block-size-mb 100");

            // Conduct safety check post copy
            taskCopyCommand.Add("echo ----- SAFETY CHECKS -----");
            taskCopyCommand.Add($"e2fsck -fy {localTargetSrc} || test $? -eq 1 ");

            // Resize the share down
            taskCopyCommand.Add("echo ----- RESIZE -----");
            taskCopyCommand.Add($"resize2fs -pM {localTargetSrc}");

            // Copy over to blob stroage
            taskCopyCommand.Add("echo ----- COPY TO BLOB -----");
            taskCopyCommand.Add($"$AZ_BATCH_NODE_SHARED_DIR/azcopy cp '{localTargetSrc}' '{taskInput.DestBlobUriWithSas}' --block-size-mb 100");

            // Delete copied file from local disk
            taskCopyCommand.Add("echo ----- DELETE USED FILE -----");
            taskCopyCommand.Add($"rm -f {localTargetSrc}");

            // Define the task
            var taskCommandLine = $"/bin/bash -cxe \"printenv && {string.Join(" && ", taskCopyCommand)}\"";

            var task = new CloudTask(taskId, taskCommandLine)
            {
                Constraints = new TaskConstraints(maxTaskRetryCount: 0, maxWallClockTime: TimeSpan.FromMinutes(TaskTimeoutMin), retentionTime: TimeSpan.FromDays(7)),
                UserIdentity = new UserIdentity(new AutoUserSpecification(elevationLevel: ElevationLevel.Admin)),
            };

            return task;
        }

        /// <inheritdoc/>
        protected override string GetBatchJobIdFromInput(BatchArchiveFileShareJobInput taskInput, int jobSuffix)
        {
            return $"{StorageProviderSettings.WorkerBatchPoolId}_archive_{jobSuffix}";
        }

        /// <inheritdoc/>
        protected override string GetBatchTaskIdFromInput(BatchArchiveFileShareJobInput taskInput, AzureResourceInfo azureResourceInfo)
        {
            // Given that we might rerun the archive job for a given source resource (in the case of a failure),
            // we need to make sure that the task ids are unique.
            return base.GetBatchTaskIdFromInput(taskInput, azureResourceInfo) + "_" + DateTime.UtcNow.Ticks;
        }
    }
}
