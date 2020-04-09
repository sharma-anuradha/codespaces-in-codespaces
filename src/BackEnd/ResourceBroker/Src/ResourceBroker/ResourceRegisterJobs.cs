// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common.Warmup;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class ResourceRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegisterJobs"/> class.
        /// </summary>
        /// <param name="deleteResourceGroupDeploymentsTask">Task to delete resource group deployments.</param>
        /// <param name="watchPoolSizeJob">Target watch pool size job.</param>
        /// <param name="watchPoolVersionTask">Target watch pool version job.</param>
        /// <param name="watchPoolStateTask">Target watch pool state task.</param>
        /// <param name="watchFailedResourcesTask">Target watch failed resources job.</param>
        /// <param name="watchOrphanedAzureResourceTask">Target watch orphaned Azure resources job.</param>
        /// <param name="watchOrphanedVmAgentImagesTask">Target watch orphaned VM images/blobs job.</param>
        /// <param name="watchOrphanedStorageImagesTask">Target watch orphaned storage images/blobs job.</param>
        /// <param name="watchOrphanedComputeImagesTask">Target watch orphaned compute images job.</param>
        /// <param name="watchOrphanedSystemResourceTask">Target watch orphaned system resources job.</param>
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public ResourceRegisterJobs(
            IDeleteResourceGroupDeploymentsTask deleteResourceGroupDeploymentsTask,
            IWatchPoolSizeTask watchPoolSizeJob,
            IWatchPoolVersionTask watchPoolVersionTask,
            IWatchPoolStateTask watchPoolStateTask,
            IWatchFailedResourcesTask watchFailedResourcesTask,
            IWatchOrphanedAzureResourceTask watchOrphanedAzureResourceTask,
            IWatchOrphanedVmAgentImagesTask watchOrphanedVmAgentImagesTask,
            IWatchOrphanedStorageImagesTask watchOrphanedStorageImagesTask,
            IWatchOrphanedComputeImagesTask watchOrphanedComputeImagesTask,
            IWatchOrphanedSystemResourceTask watchOrphanedSystemResourceTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper)
        {
            DeleteResourceGroupDeploymentsTask = deleteResourceGroupDeploymentsTask;
            WatchPoolSizeJob = watchPoolSizeJob;
            WatchPoolVersionTask = watchPoolVersionTask;
            WatchPoolStateTask = watchPoolStateTask;
            WatchFailedResourcesTask = watchFailedResourcesTask;
            WatchOrphanedAzureResourceTask = watchOrphanedAzureResourceTask;
            WatchOrphanedVmAgentImagesTask = watchOrphanedVmAgentImagesTask;
            WatchOrphanedStorageImagesTask = watchOrphanedStorageImagesTask;
            WatchOrphanedComputeImagesTask = watchOrphanedComputeImagesTask;
            WatchOrphanedSystemResourceTask = watchOrphanedSystemResourceTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
            Random = new Random();
        }

        private IDeleteResourceGroupDeploymentsTask DeleteResourceGroupDeploymentsTask { get; }

        private IWatchPoolSizeTask WatchPoolSizeJob { get; }

        private IWatchPoolVersionTask WatchPoolVersionTask { get; }

        private IWatchPoolStateTask WatchPoolStateTask { get; }

        private IWatchFailedResourcesTask WatchFailedResourcesTask { get; }

        private IWatchOrphanedAzureResourceTask WatchOrphanedAzureResourceTask { get; }

        private IWatchOrphanedVmAgentImagesTask WatchOrphanedVmAgentImagesTask { get; }

        private IWatchOrphanedStorageImagesTask WatchOrphanedStorageImagesTask { get; }

        private IWatchOrphanedComputeImagesTask WatchOrphanedComputeImagesTask { get; }

        private IWatchOrphanedSystemResourceTask WatchOrphanedSystemResourceTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public async Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{ResourceLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger),
                autoLogOperation: false);

            // Job: Populate continuation message cache
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.ContinuationTaskMessagePump}_run_try_populate_cache",
                (childLogger) => ContinuationTaskMessagePump.RunTryPopulateCacheAsync(childLogger),
                TimeSpan.FromSeconds(10));

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(1000, 2000));

            // Job: Watch Pool Size
            var watchPoolSizeTaskTimeSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolSizeTask}_run",
                (childLogger) => WatchPoolSizeJob.RunAsync(watchPoolSizeTaskTimeSpan, childLogger),
                watchPoolSizeTaskTimeSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool Version
            var watchPoolVersionTaskSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolVersionTask}_run",
                (childLogger) => WatchPoolVersionTask.RunAsync(watchPoolVersionTaskSpan, childLogger),
                watchPoolVersionTaskSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool State
            var watchPoolStateTaskSpan = TimeSpan.FromMinutes(1);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolStateTask}_run",
                (childLogger) => WatchPoolStateTask.RunAsync(watchPoolStateTaskSpan, childLogger),
                watchPoolStateTaskSpan);

            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Failed Resources
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchFailedResourcesTask}_run",
                (childLogger) => WatchFailedResourcesTask.RunAsync(TimeSpan.FromMinutes(30), childLogger),
                TimeSpan.FromMinutes(5));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Orphaned Azure Resources
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedAzureResourceTask}_run",
                (childLogger) => WatchOrphanedAzureResourceTask.RunAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Orphaned Azure Resources
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedSystemResourceTask}_run",
                (childLogger) => WatchOrphanedSystemResourceTask.RunAsync(TimeSpan.FromHours(2), childLogger),
                TimeSpan.FromMinutes(20));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Resource Group Deployments
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.DeleteResourceGroupDeploymentsTask}_run",
                (childLogger) => DeleteResourceGroupDeploymentsTask.RunAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Artifact images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedVmAgentImagesTask}_run",
                (childLogger) => WatchOrphanedVmAgentImagesTask.RunAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Artifact storage images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedStorageImagesTask}_run",
                (childLogger) => WatchOrphanedStorageImagesTask.RunAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));

            // Offset to help distribute inital load of recurring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Delete Artifact Nexus windows images
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchOrphanedComputeImagesTask}_run",
                (childLogger) => WatchOrphanedComputeImagesTask.RunAsync(TimeSpan.FromDays(1), childLogger),
                TimeSpan.FromHours(1));
        }
    }
}
