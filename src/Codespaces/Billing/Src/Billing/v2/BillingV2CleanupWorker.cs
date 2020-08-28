// <copyright file="BillingV2CleanupWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
    public class BillingV2CleanupWorker : BillingV2BaseWorker
    {
        private const string LogBaseName = "billing_v2_cleanup_worker";
        private readonly BillingSettings billingSettings;
        private readonly IBillSummaryScrubber billSummaryScrubber;
        private readonly IDiagnosticsLogger logger;

        public BillingV2CleanupWorker(
            BillingSettings billingSettings,
            IControlPlaneInfo controlPlaneInfo,
            IPlanManager planManager,
            IBillSummaryScrubber billSummaryScrubber,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IDiagnosticsLogger logger) : base(billingSettings, controlPlaneInfo, planManager, claimedDistributedLease, taskHelper)
        {
            this.billingSettings = billingSettings;
            this.billSummaryScrubber = billSummaryScrubber;
            this.logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_execute",
                async (childLogger) =>
                {
                    // align the first run with 15 minutes past the next hour
                    var initialStartTime = DateTime.UtcNow;
                    var firstRunTime = new DateTime(initialStartTime.Year, initialStartTime.Month, initialStartTime.Day, initialStartTime.Hour + 1, 15, 0, DateTimeKind.Utc);
                    var firstRunDelay = firstRunTime - initialStartTime;

                    await Task.Delay(firstRunDelay);

                    // no distributed lease is taken at the top level to allow multiple workers
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var now = DateTime.UtcNow;

                        // 15 minutes past the hour
                        var desiredEndDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, 15, 0, DateTimeKind.Utc);

                        await RunAsync(childLogger, desiredEndDate, cancellationToken);

                        var nextRun = desiredEndDate.AddHours(1) - DateTime.UtcNow;
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
                        // run scrubbers (defers this less time-sensitive work, at the cost of looping through all plans again later).
                        await ForEachPlan(
                            "cleanup",
                            async (plan, innerLogger) =>
                            {
                                await billSummaryScrubber.ScrubBillSummariesForPlan(
                                new BillScrubberRequest
                                {
                                    PlanId = plan.Id,
                                    DesiredEndTime = desiredEndDate,
                                },
                                innerLogger);
                            },
                            childLogger,
                            cancellationToken);
                    }
                },
                swallowException: true);
        }
    }
}
