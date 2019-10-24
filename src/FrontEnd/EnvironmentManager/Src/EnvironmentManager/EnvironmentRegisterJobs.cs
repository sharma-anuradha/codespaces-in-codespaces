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
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public EnvironmentRegisterJobs(
            IWatchOrphanedSystemEnvironmentsTask watchOrphanedSystemEnvironmentsTask,
            ITaskHelper taskHelper)
        {
            WatchOrphanedSystemEnvironmentsTask = watchOrphanedSystemEnvironmentsTask;
            TaskHelper = taskHelper;
        }

        private IWatchOrphanedSystemEnvironmentsTask WatchOrphanedSystemEnvironmentsTask { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Watch Orphaned Azure Resources
            TaskHelper.RunBackgroundLoop(
                $"{EnvironmentLoggingConstants.WatchOrphanedSystemEnvironmentsTask}_run",
                (childLogger) => WatchOrphanedSystemEnvironmentsTask.RunAsync(TimeSpan.FromHours(1), childLogger),
                TimeSpan.FromMinutes(10));

            return Task.CompletedTask;
        }
    }
}
