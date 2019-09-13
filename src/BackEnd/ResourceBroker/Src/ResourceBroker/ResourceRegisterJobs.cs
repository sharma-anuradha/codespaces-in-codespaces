// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
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
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public ResourceRegisterJobs(
            IWatchPoolSizeTask watchPoolSizeJob,
            IWatchPoolVersionTask watchPoolVersionTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper)
        {
            WatchPoolSizeJob = watchPoolSizeJob;
            WatchPoolVersionTask = watchPoolVersionTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
        }

        private IWatchPoolSizeTask WatchPoolSizeJob { get; }

        private IWatchPoolVersionTask WatchPoolVersionTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task WarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Populate continuation message cache
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.ContinuationTaskMessagePump}_run_try_populate_cache",
                (childLogger) => ContinuationTaskMessagePump.RunTryPopulateCacheAsync(childLogger),
                TimeSpan.FromSeconds(10));

            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{ResourceLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger));

            // Job: Watch Pool Size
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolSizeTask}_run",
                (childLogger) => WatchPoolSizeJob.RunAsync(childLogger),
                TimeSpan.FromMinutes(1));

            // Job: Watch Pool Version
            TaskHelper.RunBackgroundLoop(
                $"{ResourceLoggingConstants.WatchPoolVersionTask}_run",
                (childLogger) => WatchPoolVersionTask.RunAsync(childLogger),
                TimeSpan.FromMinutes(1));

            return Task.CompletedTask;
        }
    }
}
