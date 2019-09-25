// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
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
        /// <param name="watchPoolSizeJob">Target watch pool size job.</param>
        /// <param name="watchPoolVersionTask">Target watch pool version job.</param>
        /// <param name="watchPoolStateTask">Target watch pool state task.</param>
        /// <param name="watchPoolSettingsTask">Target watch pool settings task.</param>
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public ResourceRegisterJobs(
            IWatchPoolSizeTask watchPoolSizeJob,
            IWatchPoolVersionTask watchPoolVersionTask,
            IWatchPoolStateTask watchPoolStateTask,
            IWatchPoolSettingsTask watchPoolSettingsTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper)
        {
            WatchPoolSizeJob = watchPoolSizeJob;
            WatchPoolVersionTask = watchPoolVersionTask;
            WatchPoolStateTask = watchPoolStateTask;
            WatchPoolSettingsTask = watchPoolSettingsTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
            Random = new Random();
        }

        private IWatchPoolSizeTask WatchPoolSizeJob { get; }

        private IWatchPoolVersionTask WatchPoolVersionTask { get; }

        private IWatchPoolStateTask WatchPoolStateTask { get; }

        private IWatchPoolSettingsTask WatchPoolSettingsTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public async Task WarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{ResourceLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger));

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
            var watchPoolVersionTaskSpan = TimeSpan.FromMinutes(2);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolVersionTask}_run",
                (childLogger) => WatchPoolVersionTask.RunAsync(watchPoolVersionTaskSpan, childLogger),
                watchPoolVersionTaskSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool State
            var watchPoolStateTaskSpan = TimeSpan.FromMinutes(2);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolStateTask}_run",
                (childLogger) => WatchPoolStateTask.RunAsync(watchPoolStateTaskSpan, childLogger),
                watchPoolStateTaskSpan);

            // Offset to help distribute inital load of recuring tasks
            await Task.Delay(Random.Next(5000, 7500));

            // Job: Watch Pool Settings
            var watchPoolSettingsTaskSpan = TimeSpan.FromMinutes(2);
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolSettingsTask}_run",
                (childLogger) => WatchPoolSettingsTask.RunAsync(childLogger),
                watchPoolSettingsTaskSpan);
        }
    }
}
