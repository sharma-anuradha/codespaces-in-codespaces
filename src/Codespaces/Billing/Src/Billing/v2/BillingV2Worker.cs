// <copyright file="BillingV2Worker.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// A background worker for billing V2.
    /// </summary>
    public class BillingV2Worker : BackgroundService
    {
        private const string LogBaseName = "billing_v2_worker";
        private const string LeaseContainer = "billing-leases";

        private readonly BillingSettings billingSettings;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly IBillingOverrideRepository billingOverrideRepository;
        private readonly IPlanManager planManager;
        private readonly IBillSummaryGenerator billSummaryGenerator;
        private readonly IBillSummaryScrubber billSummaryScrubber;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly ITaskHelper taskHelper;
        private readonly IDiagnosticsLogger logger;

        private readonly IEnumerable<string> shards = new[] { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

        public BillingV2Worker(
            BillingSettings billingSettings,
            IControlPlaneInfo controlPlaneInfo,
            IBillingOverrideRepository billingOverrideRepository,
            IPlanManager planManager,
            IBillSummaryGenerator billSummaryGenerator,
            IBillSummaryScrubber billSummaryScrubber,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IDiagnosticsLogger logger)
        {
            this.billingSettings = billingSettings;
            this.controlPlaneInfo = controlPlaneInfo;
            this.billingOverrideRepository = billingOverrideRepository;
            this.planManager = planManager;
            this.billSummaryGenerator = billSummaryGenerator;
            this.billSummaryScrubber = billSummaryScrubber;
            this.claimedDistributedLease = claimedDistributedLease;
            this.taskHelper = taskHelper;
            this.logger = logger.NewChildLogger();
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_execute",
                async (childLogger) =>
                {
                    // no distributed lease is taken at the top level to allow multiple workers
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var now = DateTime.UtcNow;
                        var desiredEndDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

                        if (await billingSettings.V2WorkersAreEnabledAsync(childLogger))
                        {
                            var overrides = (await billingOverrideRepository.QueryAsync(q => q, logger)).ToList();

                            // generate billing summaries
                            await ForEachPlan(
                                "summaries",
                                async (plan, innerLogger) =>
                                {
                                    innerLogger.FluentAddValue(BillingLoggingConstants.BillEndingTime, desiredEndDate);

                                    await billSummaryGenerator.GenerateBillingSummaryAsync(
                                        new BillingSummaryRequest
                                        {
                                            PlanId = plan.Id,
                                            PlanInformation = plan.Plan,
                                            DesiredEndTime = desiredEndDate,
                                            BillingOverrides = BuildOverrides(Guid.Parse(plan.Id), plan.Plan.Subscription, overrides),
                                            Partner = plan.Partner,
                                        },
                                        innerLogger);
                                },
                                logger,
                                cancellationToken);

                            // pause to give roughly enough time for all workers to complete the first phase.
                            await Task.Delay(TimeSpan.FromMinutes(15));

                            // run scrubbers (defers this less time-sensitive work, at the cost of looping through all plans again later).
                            await ForEachPlan(
                                "scrubber",
                                async (plan, innerLogger) =>
                                {
                                    innerLogger.FluentAddValue(BillingLoggingConstants.BillEndingTime, desiredEndDate);

                                    await billSummaryScrubber.ScrubBillSummariesForPlan(
                                    new BillScrubberRequest
                                    {
                                        PlanId = plan.Id,
                                        DesiredEndTime = desiredEndDate,
                                    },
                                    innerLogger);
                                },
                                logger,
                                cancellationToken);
                        }

                        var nextRun = desiredEndDate.AddHours(1) - DateTime.UtcNow;
                        await Task.Delay(nextRun > TimeSpan.Zero ? nextRun : TimeSpan.Zero);
                    }
                });
        }

        private Task ForEachPlan(string leaseName, Func<VsoPlan, IDiagnosticsLogger, Task> action, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_execute_inner",
                (childLogger) =>
                {
                    var shardRegionPairs = shards.SelectMany(x => controlPlaneInfo.Stamp.DataPlaneLocations, (shard, location) => (shard, location)).Shuffle();

                    // execute with default of three shards at once, 250ms pause between each, with a per-shard lease
                    return taskHelper.RunConcurrentEnumerableAsync(
                        $"{LogBaseName}_execute_inner",
                        shardRegionPairs,
                        (pair, itemLogger) =>
                        {
                            itemLogger.FluentAddValue(BillingLoggingConstants.Shard, pair.shard);
                            itemLogger.FluentAddValue(BillingLoggingConstants.Location, pair.location);

                            cancellationToken.ThrowIfCancellationRequested();

                            return ExecuteShard(action, pair.shard, pair.location, itemLogger, cancellationToken);
                        },
                        childLogger,
                        (pair, itemLogger) =>
                        {
                            return claimedDistributedLease.Obtain(LeaseContainer, $"billing-v2-{leaseName}-{pair.location}-{pair.shard}", TimeSpan.FromMinutes(45), itemLogger);
                        });
                });
        }

        private Task ExecuteShard(Func<VsoPlan, IDiagnosticsLogger, Task> action, string shard, AzureLocation location, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // retrieve plans from Cosmos DB in batches of 100
            return planManager.GetBillablePlansByShardAsync(
                shard,
                location,
                (plan, childLogger) => Task.CompletedTask, // no per-item work
                (plans, childLogger) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    return ExecutePlanBatch(action, plans, childLogger, cancellationToken);
                },
                logger);
        }

        private Task ExecutePlanBatch(Func<VsoPlan, IDiagnosticsLogger, Task> action, IEnumerable<VsoPlan> plans, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // concurrently process up to 100 plans (4 at a time by default)
            return taskHelper.RunConcurrentEnumerableAsync(
                $"{LogBaseName}_worker_page",
                plans,
                async (plan, childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, plan.Id);

                    cancellationToken.ThrowIfCancellationRequested();

                    // lastly, execute the action
                    await action(plan, childLogger);                   
                },
                logger,
                concurrentLimit: billingSettings.ConcurrentJobConsumerCount,
                successDelay: 0);
        }

        private IEnumerable<BillingPlanSummaryOverrideJobPayload> BuildOverrides(
            Guid planId,
            string subscription,
            IEnumerable<BillingOverride> overrides)
        {
            var globalOverrides = overrides.Where(x => x.PlanId == null && x.Subscription == null).Select(SelectOverridePayload).ToList();
            var planOverrides = overrides.Where(x => x.PlanId != null).GroupBy(x => x.PlanId).ToDictionary(x => x.Key.Value, x => x.ToList().Select(SelectOverridePayload));
            var subscriptionOverrides = overrides.Where(x => x.Subscription != null).GroupBy(x => x.Subscription).ToDictionary(x => x.Key, x => x.ToList().Select(SelectOverridePayload));

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
