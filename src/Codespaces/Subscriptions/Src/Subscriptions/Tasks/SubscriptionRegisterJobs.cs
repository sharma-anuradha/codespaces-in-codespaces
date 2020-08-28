// <copyright file="SubscriptionRegisterJobs.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Subscription Register Jobs.
    /// </summary>
    public class SubscriptionRegisterJobs : IAsyncBackgroundWarmup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRegisterJobs"/> class.
        /// </summary>
        /// <param name="updateSubscriptionDetailsTask">Task to update subscription details.</param>
        /// <param name="bannedSubscriptionTask">Banned subscription task.</param>
        /// <param name="taskHelper">The task helper that runs the scheduled jobs.</param>
        public SubscriptionRegisterJobs(
            IUpdateSubscriptionDetailsTask updateSubscriptionDetailsTask,
            IBannedSubscriptionTask bannedSubscriptionTask,
            ITaskHelper taskHelper)
        {
            UpdateSubscriptionDetailsTask = updateSubscriptionDetailsTask;
            BannedSubscrciptionTask = bannedSubscriptionTask;
            TaskHelper = taskHelper;
        }

        private IUpdateSubscriptionDetailsTask UpdateSubscriptionDetailsTask { get; }

        private IBannedSubscriptionTask BannedSubscrciptionTask { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public Task BackgroundWarmupCompletedAsync(IDiagnosticsLogger logger)
        {
            // Job: Update subscription details.
            TaskHelper.RunBackgroundLoop(
            $"update_subscription_details_run",
            (childLogger) => UpdateSubscriptionDetailsTask.RunTaskAsync(TimeSpan.FromHours(10), childLogger),
            TimeSpan.FromHours(4));

            TaskHelper.RunBackgroundLoop(
            $"banned_worker_run",
            (childLogger) => BannedSubscrciptionTask.RunTaskAsync(TimeSpan.FromHours(1), childLogger),
            TimeSpan.FromMinutes(10));

            return Task.CompletedTask;
        }
    }
}
