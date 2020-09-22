// <copyright file="BillingV2SummaryWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    /// A worker for Billing V2 Summaries
    /// </summary>
    public class BillingV2SummaryWorker : BillingV2BaseWorker
    {
        private const string LogBaseName = "billing_v2_summary_worker";
        private readonly BillingSettings billingSettings;
        private readonly IBillingOverrideRepository billingOverrideRepository;
        private readonly IBillSummaryGenerator billSummaryGenerator;
        private readonly IDiagnosticsLogger logger;

        public BillingV2SummaryWorker(
            BillingSettings billingSettings,
            IControlPlaneInfo controlPlaneInfo,
            IBillingOverrideRepository billingOverrideRepository,
            IPlanManager planManager,
            IBillSummaryGenerator billSummaryGenerator,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IDiagnosticsLogger logger)
            : base(billingSettings, controlPlaneInfo, planManager, claimedDistributedLease, taskHelper)
        {
            this.billingSettings = billingSettings;
            this.billingOverrideRepository = billingOverrideRepository;
            this.billSummaryGenerator = billSummaryGenerator;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_execute",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("BillingInstanceId", Guid.NewGuid());

                    // align the first run with the top of the next hour
                    var initialStartTime = DateTime.UtcNow;
                    var firstRunTime = new DateTime(initialStartTime.Year, initialStartTime.Month, initialStartTime.Day, initialStartTime.Hour, 1, 0, DateTimeKind.Utc).AddHours(1);
                    var firstRunDelay = firstRunTime - initialStartTime;

                    await Task.Delay(firstRunDelay);

                    // no distributed lease is taken at the top level to allow multiple workers
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var now = DateTime.UtcNow;

                        // top of the next hour
                        var desiredEndDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

                        await RunAsync(childLogger, desiredEndDate, cancellationToken);

                        var nextRun = desiredEndDate.AddHours(1).AddMinutes(1) - DateTime.UtcNow;
                        await Task.Delay(nextRun > TimeSpan.Zero ? nextRun : TimeSpan.Zero);
                    }
                });
        }

        private Task RunAsync(IDiagnosticsLogger logger, DateTime desiredEndDate, CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("BillingRunId", Guid.NewGuid());
                    childLogger.FluentAddBaseValue(BillingLoggingConstants.BillEndingTime, desiredEndDate);

                    childLogger.LogInfo($"{LogBaseName}_run_start");

                    if (await billingSettings.V2WorkersAreEnabledAsync(logger))
                    {
                        var overrides = (await billingOverrideRepository.QueryAsync(q => q, childLogger)).ToList();

                        // generate billing summaries
                        await ForEachPlan(
                            "summary",
                            async (plan, innerLogger) =>
                            {
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
                            childLogger,
                            cancellationToken);
                    }
                },
                swallowException: true);
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
