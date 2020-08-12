// <copyright file="BillSummaryScrubber.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Scrubs a plan's summaries and environment state changes.
    /// </summary>
    public class BillSummaryScrubber : IBillSummaryScrubber
    {
        private const int NumberOfBillSummariesToKeep = 6;

        private const string LogBaseName = "bill_summary_scrubber";

        /// <summary>
        /// Initializes a new instance of the <see cref="BillSummaryScrubber"/> class.
        /// </summary>
        /// <param name="billSummaryManager">the bill summary manager used for getting bill summaires.</param>
        /// <param name="environmentStateChangeManager">the environment state change manager used for getting previous billing state changes.</param>
        /// <param name="billingArchivalManager">the archival manager.</param>
        public BillSummaryScrubber(
            IBillSummaryManager billSummaryManager,
            IEnvironmentStateChangeManager environmentStateChangeManager,
            IBillingArchivalManager billingArchivalManager)
        {
            BillSummaryManager = billSummaryManager;
            EnvironmentStateChangeManager = environmentStateChangeManager;
            BillingArchivalManager = billingArchivalManager;
        }

        private IBillSummaryManager BillSummaryManager { get; }

        private IEnvironmentStateChangeManager EnvironmentStateChangeManager { get; }

        private IBillingArchivalManager BillingArchivalManager { get; }

        /// <inheritdoc />
        public Task ScrubBillSummariesForPlan(BillScrubberRequest request, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_scrub_bill_summaries_for_plan",
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue(BillingLoggingConstants.PlanId, request.PlanId)
                               .FluentAddBaseValue("DesiredEndTime", request.DesiredEndTime);
                    var allSummaries = await BillSummaryManager.GetAllSummaries(request.PlanId, request.DesiredEndTime, childLogger.NewChildLogger());
                    var allEnvironmentStateChanges = await EnvironmentStateChangeManager.GetAllStateChanges(request.PlanId, request.DesiredEndTime, childLogger.NewChildLogger());
                    var latestSummary = allSummaries.OrderBy(x => x.PeriodEnd).LastOrDefault();
                    if (latestSummary != null)
                    {
                        /*1. Look for errors in past bill Summaries
                            1.1 Get the most recent summary.
                            1.2 Go through all environments. Find ones that are not in the most recent summary.
                        */

                        childLogger.AddVsoPlanInfo(latestSummary.Plan);

                        var changed = await CheckForMissingEnvironmentsAsync(latestSummary, latestSummary.PeriodEnd, allEnvironmentStateChanges, childLogger.NewChildLogger());
                        childLogger.FluentAddValue("AddedMissingEnvironment", changed);
                        if (changed)
                        {
                            await BillSummaryManager.CreateOrUpdateAsync(latestSummary, childLogger.NewChildLogger());
                        }

                        // Archive old summaries.
                        var olderSummaries = allSummaries.OrderByDescending(x => x.PeriodEnd).Skip(NumberOfBillSummariesToKeep);
                        childLogger.FluentAddValue("NumberOfSummariesBeingArchived", olderSummaries.Count());
                        foreach (var summary in olderSummaries)
                        {
                            await BillingArchivalManager.MigrateBillSummary(summary, childLogger.NewChildLogger());
                        }

                        // Archive old state changes.
                        var olderEnvironmentStateChanges = await FindOlderEnvironmentStateChangesAsync(latestSummary.PeriodEnd, allEnvironmentStateChanges, childLogger.NewChildLogger());
                        childLogger.FluentAddValue("NumberOfStateChangesBeingArchived", olderEnvironmentStateChanges.Count())
                            .FluentAddValue("NumberOfEnvironmentsArchived", olderEnvironmentStateChanges.GroupBy(x => x.Environment.Id).Count());
                        foreach (var olderStateChanges in olderEnvironmentStateChanges)
                        {
                            await BillingArchivalManager.MigrateEnvironmentStateChange(olderStateChanges, childLogger.NewChildLogger());
                        }
                    }
                });
        }

        private Task<bool> CheckForMissingEnvironmentsAsync(BillSummary billingSummary, DateTime end, IEnumerable<EnvironmentStateChange> allEnvironmentEvents, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_check_for_missing_environments",
                (childLogger) =>
                {
                    childLogger.FluentAddValue(BillingLoggingConstants.PlanId, billingSummary.PlanId);
                    childLogger.FluentAddValue("BillSummaryId", billingSummary.Id);
                    childLogger.FluentAddValue("BillGenerationTime", billingSummary.BillGenerationTime);
                    childLogger.FluentAddValue("PeriodStart", billingSummary.PeriodStart);
                    childLogger.FluentAddValue("PeriodEnd", billingSummary.PeriodEnd);
                    childLogger.FluentAddValue("IsFinalBill", billingSummary.IsFinalBill);
                    childLogger.FluentAddValue("PlanIsDeleted", billingSummary.PlanIsDeleted);
                    childLogger.FluentAddValue("SubmissionState", billingSummary.SubmissionState);

                    // Get all the events that happened before the current billing cycle.
                    var allOlderEnvironmentEvents = allEnvironmentEvents.Where(x => x.Time < end);
                    var hasChanged = false;

                    // Group all events by their env id
                    var envsGroupedByEnvironments = allOlderEnvironmentEvents.GroupBy(x => x.Environment.Id);
                    foreach (var env in envsGroupedByEnvironments)
                    {
                        // Do we already have some sort of record of this environmnet. If so, do nothing.
                        if (billingSummary?.UsageDetail != null && billingSummary.UsageDetail.Any(x => x.Id == env.Key))
                        {
                            continue;
                        }

                        var lastEnvEventChange = BillingUtilities.GetLastBillableEventStateChange(env);
                        if (lastEnvEventChange is null)
                        {
                            // If we get this far, we could not find an appropriate billing event. Perhaps it never became available after this time range started. If so, do nothing.
                            continue;
                        }

                        // We have the latest billable state. Let's make sure it's not already a deleted environment.
                        var lastState = lastEnvEventChange.NewValue;
                        if (lastState == nameof(CloudEnvironmentState.Deleted))
                        {
                            continue;
                        }

                        // Generate a fake entry for this billing summary so we do not lose sight of the environment.
                        var envUsageDetails = new EnvironmentUsage
                        {
                            EndState = lastState,
                            Sku = lastEnvEventChange.Environment.Sku,
                            Usage = new Dictionary<string, double>(),
                            Id = env.Key,
                        };

                        logger.FluentAddValue("EndState", envUsageDetails.EndState)
                            .FluentAddValue("Sku", envUsageDetails.Sku.ToString())
                            .FluentAddValue("Id", envUsageDetails.Id)
                            .LogInfo($"{LogBaseName}_missing_environment");

                        // Add the lost environnment to the summary for future tracking.
                        billingSummary.UsageDetail.Add(envUsageDetails);
                        hasChanged = true;
                    }

                    return Task.FromResult(hasChanged);
                });
        }

        private Task<IEnumerable<EnvironmentStateChange>> FindOlderEnvironmentStateChangesAsync(DateTime end, IEnumerable<EnvironmentStateChange> allEnvironmentEvents, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_find_older_environment_state_changes",
                (childLogger) =>
                {
                    childLogger.FluentAddValue("End", end);

                    // TODO: Consider archiving all but the most recent billable event.

                    // Get all the events that happened before the current billing cycle.
                    var allOlderEnvironmentEvents = allEnvironmentEvents.Where(x => x.Time < end);

                    var eventsThatAreOld = new List<EnvironmentStateChange>();

                    // Group all events by their env id
                    var envsGroupedByEnvironments = allOlderEnvironmentEvents.GroupBy(x => x.Environment.Id);
                    foreach (var env in envsGroupedByEnvironments)
                    {
                        var lastEnvEventChange = BillingUtilities.GetLastBillableEventStateChange(env);
                        if (lastEnvEventChange is null)
                        {
                            // If we get this far, we could not find an appropriate billing event. Perhaps it never became available after this time range started. If so, do nothing.
                            continue;
                        }

                        // We have the latest billable state. Let's make sure it's not already a deleted environment.
                        var lastState = lastEnvEventChange.NewValue;
                        if (lastState == nameof(CloudEnvironmentState.Deleted))
                        {
                            eventsThatAreOld.AddRange(env);
                        }
                    }

                    return Task.FromResult(eventsThatAreOld.AsEnumerable());
                });
        }
    }
}
