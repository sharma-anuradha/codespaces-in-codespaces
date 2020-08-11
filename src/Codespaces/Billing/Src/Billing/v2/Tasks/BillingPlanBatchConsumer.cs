// <copyright file="BillingPlanBatchConsumer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Billing Plan Consumer.
    /// </summary>
    public class BillingPlanBatchConsumer : JobHandlerPayloadBase<BillingPlanBatchJobPayload>, IBillingPlanBatchConsumer
    {
        private static readonly TimeSpan ExpireDelay = TimeSpan.FromMinutes(20);

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanBatchConsumer"/> class.
        /// </summary>
        /// <param name="planManager">Target Plan Manager.</param>
        /// <param name="billingOverrideRepository">Target Billing Override Repository.</param>
        /// <param name="billingPlanSummaryProducer">Target Billing Plan Summary Producer.</param>
        /// <param name="billingPlanCleanupProducer">Target Billing Plan Cleanup Producer.</param>
        /// <param name="taskHelper">Task Helper.</param>
        public BillingPlanBatchConsumer(
            IPlanManager planManager,
            IBillingOverrideRepository billingOverrideRepository,
            IBillingPlanSummaryProducer billingPlanSummaryProducer,
            IBillingPlanCleanupProducer billingPlanCleanupProducer,
            ITaskHelper taskHelper)
        {
            PlanManager = planManager;
            BillingOverrideRepository = billingOverrideRepository;
            BillingPlanSummaryProducer = billingPlanSummaryProducer;
            BillingPlanCleanupProducer = billingPlanCleanupProducer;
            TaskHelper = taskHelper;
        }

        private IPlanManager PlanManager { get; }

        private IBillingOverrideRepository BillingOverrideRepository { get; }

        private IBillingPlanSummaryProducer BillingPlanSummaryProducer { get; }

        private IBillingPlanCleanupProducer BillingPlanCleanupProducer { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BillingPlanBatchJobPayload payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanBatchTask}_handle",
                async (childLogger) =>
                {
                    // Fetch list of plans overrides
                    var overrides = await BillingOverrideRepository.QueryAsync(q => q, logger);
                    var globalOverrides = overrides.Where(x => x.PlanId == null && x.Subscription == null).Select(SelectOverridePayload);
                    var planOverrides = overrides.Where(x => x.PlanId != null).GroupBy(x => x.PlanId).ToDictionary(x => x.Key.Value, x => x.ToList().Select(SelectOverridePayload));
                    var subscriptionOverrides = overrides.Where(x => x.Subscription != null).GroupBy(x => x.Subscription).ToDictionary(x => x.Key, x => x.ToList().Select(SelectOverridePayload));

                    // Fetch list of plans
                    await PlanManager.GetBillablePlansByShardAsync(
                        payload.PlanShard,
                        async (plan, childLogger) =>
                        {
                            // nothing to be done per item
                            await Task.CompletedTask;
                        },
                        async (IEnumerable<VsoPlan> plans, IDiagnosticsLogger childLogger) =>
                        {
                            await QueueJobs(plans, globalOverrides, planOverrides, subscriptionOverrides, childLogger, cancellationToken);
                        },
                        logger);
                });
        }

        private async Task QueueJobs(IEnumerable<VsoPlan> plans, IEnumerable<BillingPlanSummaryOverrideJobPayload> globalOverrides, Dictionary<Guid, IEnumerable<BillingPlanSummaryOverrideJobPayload>> planOverrides, Dictionary<string, IEnumerable<BillingPlanSummaryOverrideJobPayload>> subscriptionOverrides, IDiagnosticsLogger childLogger, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            foreach (var plan in plans)
            {
                // Set messages to become visable at the time of the hour
                var currentTimeOfDay = DateTime.UtcNow.TimeOfDay;
                var nextFullHour = TimeSpan.FromHours(Math.Ceiling(currentTimeOfDay.TotalHours));
                var initialVisibilityDelay = nextFullHour - currentTimeOfDay;
                var nextHour = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0).AddHours(1);

                tasks.Add(BillingPlanSummaryProducer.PublishJobAsync(
                            plan.Id,
                            plan.Plan,
                            nextHour,
                            BuildOverrides(Guid.Parse(plan.Id), plan.Plan.Subscription, globalOverrides, planOverrides, subscriptionOverrides),
                            new JobPayloadOptions { InitialVisibilityDelay = initialVisibilityDelay, ExpireTimeout = initialVisibilityDelay + ExpireDelay },
                            childLogger.NewChildLogger(),
                            cancellationToken));

                // also queue cleanup and archiving job
                var cleanupDelay = initialVisibilityDelay.Add(TimeSpan.FromMinutes(20));

                tasks.Add(BillingPlanCleanupProducer.PublishJobAsync(
                    plan.Id,
                    nextHour,
                    new JobPayloadOptions { InitialVisibilityDelay = cleanupDelay, ExpireTimeout = cleanupDelay + ExpireDelay },
                    childLogger.NewChildLogger(),
                    cancellationToken));
            }

            await TaskHelper.RunConcurrentEnumerableAsync(
                $"{BillingLoggingConstants.BillingPlanBatchTask}_queue",
                tasks,
                (task, childLogger) => task,
                childLogger,
                concurrentLimit: 25,
                successDelay: 0);
        }

        private IEnumerable<BillingPlanSummaryOverrideJobPayload> BuildOverrides(
            Guid planId,
            string subscription,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> globalOverrides,
            IDictionary<Guid, IEnumerable<BillingPlanSummaryOverrideJobPayload>> planOverrides,
            IDictionary<string, IEnumerable<BillingPlanSummaryOverrideJobPayload>> subscriptionOverrides)
        {
            return globalOverrides
                .Concat(planOverrides.GetValueOrDefault(planId, () => Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>()))
                .Concat(subscriptionOverrides.GetValueOrDefault(subscription, () => Enumerable.Empty<BillingPlanSummaryOverrideJobPayload>()));
        }

        private BillingPlanSummaryOverrideJobPayload SelectOverridePayload(BillingOverride x)
        {
            return new BillingPlanSummaryOverrideJobPayload { Priority = x.Priority, BillingOverrideState = x.BillingOverrideState, EndTime = x.EndTime, StartTime = x.StartTime, Sku = x.Sku };
        }
    }
}
