// <copyright file="EnvironmentRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment Register Jobs.
    /// </summary>
    public class EnvironmentRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentRegisterJobs"/> class.
        /// </summary>
        /// <param name="watchOrphanedSystemEnvironmentsTask">Target watch orphaned system environments task.</param>
        /// <param name="logCloudEnvironmentStateTask">Target Log Cloud Environment State task.</param>
        /// <param name="logSubscriptionStatisticsTask">Target Log Subscriptions Statistics task.</param>
        /// <param name="continuationTaskMessagePump">Target Continuation Task Message Pump.</param>
        /// <param name="continuationTaskWorkerPoolManager">Target Continuation Task Worker Pool Manager.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public EnvironmentRegisterJobs(
            IWatchOrphanedSystemEnvironmentsTask watchOrphanedSystemEnvironmentsTask,
            ILogCloudEnvironmentStateTask logCloudEnvironmentStateTask,
            ILogSubscriptionStatisticsTask logSubscriptionStatisticsTask,
            IContinuationTaskMessagePump continuationTaskMessagePump,
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            ITaskHelper taskHelper)
        {
            WatchOrphanedSystemEnvironmentsTask = watchOrphanedSystemEnvironmentsTask;
            LogCloudEnvironmentStateTask = logCloudEnvironmentStateTask;
            LogSubscriptionStatisticsTask = logSubscriptionStatisticsTask;
            ContinuationTaskMessagePump = continuationTaskMessagePump;
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            TaskHelper = taskHelper;
        }

        private IWatchOrphanedSystemEnvironmentsTask WatchOrphanedSystemEnvironmentsTask { get; }

        private ILogCloudEnvironmentStateTask LogCloudEnvironmentStateTask { get; }

        private ILogSubscriptionStatisticsTask LogSubscriptionStatisticsTask { get; }

        private IContinuationTaskMessagePump ContinuationTaskMessagePump { get; }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Continuation Task Worker Pool Manager
            TaskHelper.RunBackground(
                $"{EnvironmentLoggingConstants.ContinuationTaskWorkerPoolManager}_start",
                (childLogger) => ContinuationTaskWorkerPoolManager.StartAsync(childLogger),
                autoLogOperation: false);

            // Job: Populate continuation message cache
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.ContinuationTaskMessagePump}_run_try_populate_cache",
                (childLogger) => ContinuationTaskMessagePump.RunTryPopulateCacheAsync(childLogger),
                TimeSpan.FromSeconds(10));

            // Job: Watch Orphaned Azure Resources
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchOrphanedSystemEnvironmentsTask}_run",
                (childLogger) => WatchOrphanedSystemEnvironmentsTask.RunAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            // Job: Log Cloud Environment State
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.LogCloudEnvironmentsStateTask}_run",
                (childLogger) => LogCloudEnvironmentStateTask.RunAsync(TimeSpan.FromMinutes(10), childLogger),
                TimeSpan.FromMinutes(1));

            // Job: Log Plan and Subscription Information
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.LogSubscriptionStatisticsTask}_run",
                (childLogger) => LogSubscriptionStatisticsTask.RunAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            return Task.CompletedTask;
        }
    }
}
