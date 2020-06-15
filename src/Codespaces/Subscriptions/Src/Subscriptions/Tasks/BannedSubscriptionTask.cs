// <copyright file="BannedSubscriptionTask.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Subscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions
{
    /// <summary>
    /// Background task for processing banned subscriptions.
    /// </summary>
    public class BannedSubscriptionTask : IBannedSubscriptionTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BannedSubscriptionTask"/> class.
        /// </summary>
        /// <param name="subscriptionManager">The subscription manager.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="taskHelper">The task helper.</param>
        /// <param name="claimedDistributedLease">The distributed lease helpers.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public BannedSubscriptionTask(
            ISubscriptionManager subscriptionManager,
            IPlanManager planManager,
            IControlPlaneInfo controlPlaneInfo,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease)
        {
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            LocationsToProcess = new HashSet<AzureLocation>(Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo)).Stamp.DataPlaneLocations);
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
        }

        private string LogBaseName => "banned_subscription_worker";

        private string LeaseBaseName => "banned-subscription-worker";

        private ISubscriptionManager SubscriptionManager { get; }

        private IPlanManager PlanManager { get; }

        private HashSet<AzureLocation> LocationsToProcess { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc />
        public Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    await TaskHelper.RunConcurrentEnumerableAsync(
                     $"{LogBaseName}_run_unit_check",
                     LocationsToProcess.Select(x => x.ToString()),
                     (location, itemLogger) => CoreRunUnitAsync(location, itemLogger),
                     childLogger,
                     (location, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{location}", taskInterval, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task CoreRunUnitAsync(string location, IDiagnosticsLogger logger)
        {
            var recentBannedSubscriptions = await SubscriptionManager.GetRecentBannedSubscriptionsAsync(logger.NewChildLogger());
            foreach (var sub in recentBannedSubscriptions)
            {
                await DeletePlansFromBannedSubscriptionAsync(sub, location, logger.NewChildLogger());
            }
        }

        private async Task DeletePlansFromBannedSubscriptionAsync(Subscription sub, string location, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
              $"{LogBaseName}_subscription_run",
              async (childLogger) =>
              {
                  var plansForSubscription = await PlanManager.ListAsync(null, sub.Id, null, null, childLogger, includeDeleted: false);
                  if (!plansForSubscription.Any())
                  {
                      childLogger
                          .FluentAddValue(nameof(sub.Id), sub.Id)
                          .AddReason("No plans for this subscription.")
                          .LogInfo($"{LogBaseName}_skipped");
                      return;
                  }

                  var plansToProcess = plansForSubscription.Where(item => item.Plan.Location.ToString().Equals(location, StringComparison.OrdinalIgnoreCase));
                  if (!plansToProcess.Any())
                  {
                      childLogger
                          .FluentAddValue(nameof(sub.Id), sub.Id)
                          .AddReason("No plans for this location.")
                          .LogInfo($"{LogBaseName}_skipped");
                      return;
                  }

                  foreach (var plan in plansToProcess)
                  {
                      var innerLogger = childLogger.NewChildLogger();
                      innerLogger.AddVsoPlan(plan);
                      await PlanManager.DeleteAsync(plan, innerLogger);
                  }

                  // mark the subscription correctly.
                  sub = await SubscriptionManager.UpdatedCompletedBannedSubscriptionAsync(sub, childLogger.NewChildLogger());
              },
              swallowException: true);
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                "subscription_leases", leaseName, claimSpan, logger);
        }
    }
}
