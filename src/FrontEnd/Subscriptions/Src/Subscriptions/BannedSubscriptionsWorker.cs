// <copyright file="BannedSubscriptionsWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Background worker for processing banned subscriptions.
    /// </summary>
    public class BannedSubscriptionsWorker : BackgroundService
    {
        private static readonly TimeSpan ProcessBannedPlanLeaseTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="BannedSubscriptionsWorker"/> class.
        /// </summary>
        /// <param name="subscriptionManager">The subscription manager.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="claimedDistributedLease">The distributed lease helpers.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public BannedSubscriptionsWorker(
            ISubscriptionManager subscriptionManager,
            IPlanManager planManager,
            IControlPlaneInfo controlPlaneInfo,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
        {
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            LocationsToProcess = new HashSet<AzureLocation>(Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo)).Stamp.DataPlaneLocations);
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            BaseLogger = Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory)).New(defaultLogValues);
        }

        private ISubscriptionManager SubscriptionManager { get; }

        private IPlanManager PlanManager { get; }

        private HashSet<AzureLocation> LocationsToProcess { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IDiagnosticsLogger BaseLogger { get; }

        /// <summary>
        /// Executes a background Task.
        /// </summary>
        /// <param name="cancellationToken">Notification object for stopping the task.</param>
        /// <returns>Task.</returns>
        protected async override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Logging housekeeping
            var logMessageBase = GetType().FormatLogMessage(nameof(ExecuteAsync));
            var logger = BaseLogger.NewChildLogger();
            cancellationToken.Register(() => logger.LogInfo($"{logMessageBase}_cancelled"));

            // Get the list of subscriptions to act on
            var recentBannedSubscriptions = await SubscriptionManager.GetRecentBannedSubscriptionsAsync(null, logger);

            TaskHelper.RunBackgroundConcurrentEnumerable(
                "banned_subscription_worker",
                recentBannedSubscriptions,
                async (sub, childLogger) =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await DeletePlansFromBannedSubscriptionAsync(sub, childLogger);
                    }
                },
                logger);
        }

        private async Task DeletePlansFromBannedSubscriptionAsync(BannedSubscription sub, IDiagnosticsLogger logger)
        {
            var logMessageBase = GetType().FormatLogMessage(nameof(DeletePlansFromBannedSubscriptionAsync));

            var plansForSubscription = await PlanManager.ListAsync(null, sub.Id, null, null, logger, includeDeleted: false);
            if (!plansForSubscription.Any())
            {
                logger
                    .FluentAddValue(nameof(sub.Id), sub.Id)
                    .AddReason("No plans for this subscription.")
                    .LogInfo($"{logMessageBase}_skipped");
                return;
            }

            var plansToProcess = plansForSubscription.Where(item => LocationsToProcess.Contains(item.Plan.Location)).ToArray();
            if (!plansToProcess.Any())
            {
                logger
                    .FluentAddValue(nameof(sub.Id), sub.Id)
                    .AddReason("No plans for this location.")
                    .LogInfo($"{logMessageBase}_skipped");
                return;
            }

            TaskHelper.RunBackgroundConcurrentEnumerable(
                "process_plan_for_banned_subscription",
                plansToProcess,
                async (plan, childLogger) =>
                {
                    await PlanManager.DeleteAsync(plan, childLogger);
                    childLogger
                        .FluentAddValue(nameof(sub.Id), plan.Plan.Subscription)
                        .FluentAddValue("PlanId", plan.Id)
                        .FluentAddValue("PlanResourceId", plan.Plan.ResourceId)
                        .AddReason(sub.BannedReason.ToString())
                        .LogInfo($"{logMessageBase}_completed");
                },
                logger,
                async (plan, childLogger) =>
                {
                    return await ClaimedDistributedLease.Obtain("banned_subscription_plans", plan.Id, ProcessBannedPlanLeaseTime, childLogger);
                });
        }
    }
}
