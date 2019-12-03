// <copyright file="EnvironmentRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
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
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public EnvironmentRegisterJobs(
            IWatchOrphanedSystemEnvironmentsTask watchOrphanedSystemEnvironmentsTask,
            ILogCloudEnvironmenstStateTask logCloudEnvironmentStateTask,
            ITaskHelper taskHelper)
        {
            WatchOrphanedSystemEnvironmentsTask = watchOrphanedSystemEnvironmentsTask;
            LogCloudEnvironmentStateTask = logCloudEnvironmentStateTask;
            TaskHelper = taskHelper;
        }

        private IWatchOrphanedSystemEnvironmentsTask WatchOrphanedSystemEnvironmentsTask { get; }

        private ILogCloudEnvironmenstStateTask LogCloudEnvironmentStateTask { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
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

            return Task.CompletedTask;
        }
    }
}
