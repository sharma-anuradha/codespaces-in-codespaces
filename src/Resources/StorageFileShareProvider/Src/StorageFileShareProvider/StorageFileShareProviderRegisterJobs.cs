// <copyright file="StorageFileShareProviderRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Registers any jobs that need to be run on warmup.
    /// </summary>
    public class StorageFileShareProviderRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageFileShareProviderRegisterJobs"/> class.
        /// </summary>
        /// <param name="watchStorageAzureBatchCleanupTask">Watch storage Azure Batch cleanup task.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public StorageFileShareProviderRegisterJobs(
            IWatchStorageAzureBatchCleanupTask watchStorageAzureBatchCleanupTask,
            ITaskHelper taskHelper)
        {
            Requires.NotNull(taskHelper, nameof(taskHelper));

            WatchStorageAzureBatchCleanupTask = watchStorageAzureBatchCleanupTask;
            TaskHelper = taskHelper;
        }

        private IWatchStorageAzureBatchCleanupTask WatchStorageAzureBatchCleanupTask { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Clean up old Azure Batch Jobs
            TaskHelper.RunBackgroundLoop(
                $"{TaskConstants.WatchStorageAzureBatchCleanupTaskLogBaseName}_run",
                (childLogger) =>
                    WatchStorageAzureBatchCleanupTask.RunTaskAsync(TimeSpan.FromMinutes(30), childLogger),
                TimeSpan.FromMinutes(30));

            return Task.CompletedTask;
        }
    }
}
