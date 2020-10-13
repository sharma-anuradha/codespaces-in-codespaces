// <copyright file="TaskConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks
{
    /// <summary>
    /// Resource logging constants.
    /// </summary>
    public class TaskConstants
    {
        /// <summary>
        /// Lease container name.
        /// </summary>
        public const string LeaseContainerName = "storage-provider-job-leases";

        /// <summary>
        /// Base log name for WatchStorageAzureBatchCleanupTask.
        /// </summary>
        public const string WatchStorageAzureBatchCleanupTaskLogBaseName = "watch_storage_azure_batch_cleanup_task";

        /// <summary>
        /// Base log name for AzureBatchLoggerTask.
        /// </summary>
        public const string AzureBatchLoggerTaskLogBaseName = "watch_storage_azure_batch_metrics_logger_task";

        /// <summary>
        /// Displayed name for Prepare Task
        /// </summary>
        public const string PrepareTaskDisplayName = "BatchPrepareFileShare";

        /// <summary>
        /// Displayed name for Archive Task
        /// </summary>
        public const string ArchiveTaskDisplayName = "BatchArchiveFileShare";

        /// <summary>
        /// Timeout of Archive Tasks in Batch Pools
        /// </summary>
        public const int ArchiveTaskTimeoutMin = 90;

        /// <summary>
        /// Timeout of Prepare Tasks in Batch Pools
        /// </summary>
        public const int PrepareTaskTimeoutMin = 30;
    }
}
