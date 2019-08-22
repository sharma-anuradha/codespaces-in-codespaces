// <copyright file="ResourceRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Registeres any jobs that need to be run on warmup.
    /// </summary>
    public class ResourceRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Name of the queue being used.
        /// </summary>
        public const string QueueName = "recurring-queue";

        private const string Every5Minutes = "*/5 * * * *";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRegisterJobs"/> class.
        /// </summary>
        /// <param name="watchPoolSizeJob">The <see cref="WatchPoolSizeJob"/> job that
        /// needs to be run.</param>
        /// <param name="continuationTaskWorkerPoolManager"></param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public ResourceRegisterJobs(
            IWatchPoolSizeTask watchPoolSizeJob,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper)
        {
            WatchPoolSizeJob = watchPoolSizeJob;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
        }

        private IWatchPoolSizeTask WatchPoolSizeJob { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task WarmupCompletedAsync()
        {
            // Job: Watch Pool Size
            TaskHelper.RunBackgroundSchedule(
                "watch-pool-size",
                TimeSpan.FromMinutes(2),
                (childLogger) => WatchPoolSizeJob.RunAsync(childLogger));

            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                "continuation-task-queue-manage",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger));

            return Task.CompletedTask;
        }
    }
}
