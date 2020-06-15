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
    }
}
