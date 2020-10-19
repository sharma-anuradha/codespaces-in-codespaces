// <copyright file="BillSummaryGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The generator for creating bill summaries.
    /// </summary>
    public class BillSummaryGenerator : IBillSummaryGenerator
    {
        private const int BillingWindowMaximumTime = -48;
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> skuDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillSummaryGenerator"/> class.
        /// </summary>
        /// <param name="billingSettings">The billing settings.</param>
        /// <param name="billSummaryManager">The billsummary manager for getting past billing summaries.</param>
        /// <param name="environmentStateChangeManager">the environment state manager for getting environment states between a certain time range.</param>
        /// <param name="skuCatalog">the sku catalog.</param>
        /// <param name="billingMeterService"> the billing meter service.</param>
        /// <param name="billingStorageFactory">the billing storage factory.</param>
        /// <param name="partnerCloudStorageFactory">Factory for partner submissions</param>
        public BillSummaryGenerator(
            IBillSummaryManager billSummaryManager,
            IEnvironmentStateChangeManager environmentStateChangeManager,
            ISkuCatalog skuCatalog,
            IBillingMeterService billingMeterService,
            IBillingSubmissionCloudStorageFactory billingStorageFactory,
            IPartnerCloudStorageFactory partnerCloudStorageFactory)
        {
            BillSummaryManager = billSummaryManager;
            EnvironmentStateChangeManager = environmentStateChangeManager;
            BillingMeterService = billingMeterService;
            BillingStorageFactory = billingStorageFactory;
            PartnerCloudStorageFactory = partnerCloudStorageFactory;
            skuDictionary = skuCatalog.CloudEnvironmentSkus;
        }

        private IBillSummaryManager BillSummaryManager { get; }

        private IEnvironmentStateChangeManager EnvironmentStateChangeManager { get; }

        private IBillingMeterService BillingMeterService { get; }

        private IBillingSubmissionCloudStorageFactory BillingStorageFactory { get; }

        private IPartnerCloudStorageFactory PartnerCloudStorageFactory { get; }

        /// <inheritdoc />
        public Task GenerateBillingSummaryAsync(BillingSummaryRequest billingSummaryRequest, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                "billSummary_generation",
                async (childLogger) =>
                {
                    var billEndTime = billingSummaryRequest.DesiredEndTime;
                    var planId = billingSummaryRequest.PlanId;
                    var partner = billingSummaryRequest.Partner;
                    childLogger.AddVsoPlanInfo(billingSummaryRequest.PlanInformation)
                               .FluentAddBaseValue("PlanId", planId)
                               .FluentAddBaseValue("BillEndingTime", billingSummaryRequest.DesiredEndTime)
                               .FluentAddBaseValue("Partner", partner);

                    // Get the last one billing summary for the planId.
                    var lastBillSummary = await BillSummaryManager.GetLatestBillSummaryAsync(planId, childLogger);
                    var isFirstSummary = lastBillSummary == null;

                    childLogger.FluentAddValue("IsFirstSummary", isFirstSummary);

                    // If there's no previous bill, we need to have a fake one we can operate against as our source of truth.
                    if (isFirstSummary)
                    {
                        lastBillSummary = new BillSummary()
                        {
                            PeriodEnd = billEndTime.AddDays(-2),
                            UsageDetail = new List<EnvironmentUsage>(),
                            Plan = billingSummaryRequest.PlanInformation,
                        };
                    }

                    var isBillAlreadySubmitted = lastBillSummary.PeriodEnd >= billEndTime;
                    childLogger.FluentAddValue("IsBillAlreadySubmitted", isBillAlreadySubmitted);

                    // double check that this is the last billing summary and we are not duplicating a summary.
                    if (isBillAlreadySubmitted)
                    {
                        return;
                    }

                    var billStartTime = lastBillSummary.PeriodEnd;
                    var allRecentEnvironmentStateChanges = await EnvironmentStateChangeManager.GetAllRecentEnvironmentEvents(planId, billStartTime, billEndTime, childLogger.NewChildLogger());

                    // We are confident enough to generate a bill for this time period.
                    var billGenerationTime = DateTime.UtcNow;
                    childLogger.FluentAddValue("BillStartingTime", billStartTime)
                               .FluentAddValue("BillGenerationTime", billGenerationTime);

                    var billSummary = new BillSummary()
                    {
                        PeriodStart = billStartTime,
                        PeriodEnd = billEndTime,
                        Usage = new Dictionary<string, double>(),
                        UsageDetail = new List<EnvironmentUsage>(),
                        Plan = billingSummaryRequest.PlanInformation,
                        BillGenerationTime = billGenerationTime,
                        PlanId = planId,
                        Id = Guid.NewGuid().ToString(),
                    };

                    // Update the bill with environments thate had state changes
                    await UpdateBillSummaryWithEnvironmentStateChangesAsync(billSummary, allRecentEnvironmentStateChanges, lastBillSummary, billEndTime, billingSummaryRequest.BillingOverrides, childLogger.NewChildLogger(), partner);

                    // Update the bill with environments that did not have state changes
                    await UpdateBillSummaryWithEnvironmentsWithNoStateChanges(billSummary, allRecentEnvironmentStateChanges, lastBillSummary, billEndTime, billingSummaryRequest.BillingOverrides, childLogger.NewChildLogger(), partner);

                    // Add the bill
                    billSummary = await BillSummaryManager.CreateOrUpdateAsync(billSummary, childLogger.NewChildLogger());

                    // Now we need to submit this to PushAgent.
                    await TransmitBillSummaryToPushAgent(billingSummaryRequest, billSummary, billingSummaryRequest.PlanInformation, childLogger.NewChildLogger());

                    await TransmitToPartner(billingSummaryRequest, billSummary, partner, childLogger.NewChildLogger());
                },
                swallowException: true);
        }

        private async Task TransmitToPartner(BillingSummaryRequest billingSummaryRequest, BillSummary billSummary, Partner? partner, IDiagnosticsLogger logger)
        {
            if (billingSummaryRequest.EnablePartnerSubmission)
            {
                if (partner == Partner.GitHub)
                {
                    await logger.OperationScopeAsync(
                        "billSummaryGenerator_partner_bill_submission",
                        async (childLogger) =>
                        {
                            var partnerSubmission = new PartnerQueueSubmission(billSummary);
                            childLogger.FluentAddValue("billEndTime", billSummary.PeriodEnd.ToString())
                               .FluentAddValue("PartnerId", "gh")
                               .FluentAddValue("ComputeTime", partnerSubmission.TotalComputeTime)
                               .FluentAddValue("StorageTime", partnerSubmission.TotalStorageTime);

                            if (!partnerSubmission.IsEmpty())
                            {
                                var storageClient = await PartnerCloudStorageFactory.CreatePartnerCloudStorage(billSummary.Plan.Location, "gh");

                                await storageClient.PushPartnerQueueSubmission(partnerSubmission);
                                childLogger.FluentAddValue("SubmittedPartnerBill", true);
                            }
                            else
                            {
                                childLogger.FluentAddValue("SubmittedPartnerBill", false);
                            }
                        });
                }
            }
        }

        /// <summary>
        /// This method loops through all the EnvironmentStateChanges<see cref="EnvironmentStateChange"/> in this current billing period and generates a
        /// BillSummary. The last BillSummary is used as the starting point for billing unit calculations.
        /// </summary>
        /// <param name="currentBillSummary">the current bill being operated on.</param>
        /// <param name="events">the list of state changeEvents.</param>
        /// <param name="lastSummary">The previous summary.</param>
        /// <param name="endTime">The desired bill end time.</param>
        /// <param name="overrides">any billing overrides if they are present for this plan.</param>
        /// <param name="logger"> the logger.</param>
        /// <param name="partner">The Partner <see cref="Partner"/>.</param>
        /// <returns>The task indicating completion of this.</returns>
        public async Task UpdateBillSummaryWithEnvironmentStateChangesAsync(
            BillSummary currentBillSummary,
            IEnumerable<EnvironmentStateChange> events,
            BillSummary lastSummary,
            DateTime endTime,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> overrides,
            IDiagnosticsLogger logger,
            Partner? partner = null)
        {
            // Get events per environment
            var billingEventsPerEnvironment = events.GroupBy(b => b.Environment.Id);
            foreach (var environmentEvents in billingEventsPerEnvironment)
            {
                await logger.OperationScopeAsync(
                    "billSummaryGenerator_perEnvironmentWithEvents",
                    (childLogger) =>
                    {
                        var seqEvents = environmentEvents.OrderBy(t => t.Time);
                        var environmentDetails = environmentEvents.First().Environment;
                        var slices = new List<BillingWindowSlice>();

                        childLogger.AddBaseValue("CloudEnvironmentId", environmentEvents.Key);

                        // Create an initial BillingWindowSlice to use as a starting point for billing calculations
                        // The previous BillingSummary is used as the StartTime and EndTime. The environments
                        // previous state comes from the list of environment details in the previous BillingSummary.
                        var isContinuingFromPreviousSummary = lastSummary?.UsageDetail != null && lastSummary.UsageDetail.Any(x => x.Id == environmentEvents.Key);
                        childLogger.FluentAddValue("IsBillEnvironmentContinuingFromPrevious", isContinuingFromPreviousSummary);
                        var previousEnvironmentState = CloudEnvironmentState.None;
                        var previousEnvironmentSku = environmentDetails.Sku; // Default to sku of first event
                        if (isContinuingFromPreviousSummary)
                        {
                            var endPreviousEnvironment = lastSummary.UsageDetail.Single(x => x.Id == environmentEvents.Key);
                            previousEnvironmentState = ParseEnvironmentState(endPreviousEnvironment.EndState);
                            previousEnvironmentSku = endPreviousEnvironment.Sku;
                        }

                        var initialState = new BillingWindowSlice.NextState
                        {
                            EnvironmentState = previousEnvironmentState,
                            Sku = previousEnvironmentSku,
                            TransitionTime = lastSummary.PeriodEnd,
                        };
                        childLogger.FluentAddValue("PreviousEnvironmentState", previousEnvironmentState);

                        IEnumerable<BillingWindowSlice> allSlices;
                        var currState = initialState;
                        var numOfStateTransitions = seqEvents.Count();
                        childLogger.FluentAddValue("NumberOfStateTransitions", numOfStateTransitions);

                        // Loop through each billing event for the current environment.
                        foreach (var currentEvent in seqEvents)
                        {
                            (allSlices, currState) = GenerateWindowSlices(endTime, currState, currentEvent, overrides);
                            if (allSlices.Any())
                            {
                                slices.AddRange(allSlices);
                            }
                        }

                        // Get the remainder or the entire window if there were no events.
                        (allSlices, currState) = GenerateWindowSlices(endTime, currState, null, overrides);
                        if (allSlices.Any())
                        {
                            slices.AddRange(allSlices);
                        }

                        AddEnvironmentToBillSummary(currentBillSummary, endTime, seqEvents, environmentDetails.Id, slices, currState, childLogger, partner);

                        return Task.CompletedTask;
                    });
            }
        }

        /// <summary>
        /// This method will generate a BillingSummary that includes environments that did not have any billing events
        /// during the given period. The previous BillingSummary and the current BillingSummary are compared to determine
        /// if any environments exist in the previous but not in the current. For those environments a billing unit is
        /// calculated and appended to the current BillingSummary.
        /// </summary>
        /// <param name="currentBillSummary">the current bill being operated on.</param>
        /// <param name="events">the list of state changeEvents.</param>
        /// <param name="lastSummary">The previous summary.</param>
        /// <param name="end">The desired bill end time.</param>
        /// <param name="overrides">any billing overrides if they are present for this plan.</param>
        /// <param name="logger"> the logger.</param>
        /// <param name="partner">The Partner <see cref="Partner"/>.</param>
        /// <returns>a task.</returns>
        public async Task UpdateBillSummaryWithEnvironmentsWithNoStateChanges(
            BillSummary currentBillSummary,
            IEnumerable<EnvironmentStateChange> events,
            BillSummary lastSummary,
            DateTime end,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> overrides,
            IDiagnosticsLogger logger,
            Partner? partner = null)
        {
            IDictionary<string, EnvironmentUsage> currentPeriodEnvironments = currentBillSummary.UsageDetail.ToDictionary(x => x.Id);

            // Find environments that are present in the previous BillingSummary and not present in the current.
            var lastPeriodEnvironments = lastSummary.UsageDetail.ToDictionary(x => x.Id);

            // Get a list of environment Ids present in the last billing summary but not in the current summary.
            var missingEnvmtIds = lastPeriodEnvironments.Keys.Where(i => !currentPeriodEnvironments.ContainsKey(i));
            if (!missingEnvmtIds.Any())
            {
                // No missing environment Ids found.
                return;
            }

            var seqEvents = events.OrderBy(x => x.Time);
            var envUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();

            // Loop through the missing environments and calculate billing.
            var missingEnvironments = lastPeriodEnvironments.Where(i => missingEnvmtIds.Contains(i.Key));
            foreach (var environment in missingEnvironments)
            {
                await logger.OperationScopeAsync(
                    "billSummaryGenerator_perEnvironmentWithNoEvents",
                    (childLogger) =>
                    {
                        childLogger.FluentAddValue("CloudEnvironmentId", environment.Key);

                        var envUsageDetail = environment.Value;

                        if (envUsageDetail.EndState == nameof(CloudEnvironmentState.Deleted) ||
                            envUsageDetail.EndState == nameof(CloudEnvironmentState.Moved))
                        {
                            // Environment state is Deleted or Moved so nothing new to bill.
                            return Task.CompletedTask;
                        }

                        var endState = new BillingWindowSlice.NextState
                        {
                            EnvironmentState = ParseEnvironmentState(envUsageDetail.EndState),
                            Sku = envUsageDetail.Sku,
                            TransitionTime = lastSummary.PeriodEnd,
                        };

                        // Get the remainder or the entire window if there were no events.
                        var (allSlices, currState) = GenerateWindowSlices(end, endState, null, overrides);

                        AddEnvironmentToBillSummary(currentBillSummary, end, seqEvents, environment.Key, allSlices, currState, childLogger, partner);

                        return Task.CompletedTask;
                    });
            }
        }

        private void AddEnvironmentToBillSummary(BillSummary currentBillSummary, DateTime endTime, IOrderedEnumerable<EnvironmentStateChange> seqEvents, string environmentId, IEnumerable<BillingWindowSlice> slices, BillingWindowSlice.NextState finalState, IDiagnosticsLogger childLogger, Partner? partner = null)
        {
            // We might not have any billable slices. If we don't, let's not do anything
            if (slices.Any())
            {
                var (isError, errorState) = CheckForErrors(environmentId, finalState.EnvironmentState, seqEvents, childLogger);
                var resourceUsageDetail = new ResourceUsageDetail();

                CloudEnvironmentState finalEnvironmentStateOrError;
                if (!isError)
                {
                    finalEnvironmentStateOrError = finalState.EnvironmentState;

                    // Aggregate the billable units for each slice created above.
                    var hasOverridenSlices = false;
                    foreach (var slice in slices)
                    {
                        if (slice.OverrideState == BillingOverrideState.BillingDisabled)
                        {
                            hasOverridenSlices = true;
                        }

                        CalculateResourceUsageByTimeAndSku(slice, resourceUsageDetail);
                    }

                    childLogger.FluentAddValue("EnvironmentHasOverridenSlices", hasOverridenSlices);
                }
                else
                {
                    finalEnvironmentStateOrError = errorState;
                    childLogger.FluentAddValue("HasIncorrectBillingState", true);
                }

                var envUsageDictionary = BillingMeterService.GetUsageBasedOnResources(resourceUsageDetail, currentBillSummary.Plan, endTime, childLogger, partner);

                var envUsageDetail = new EnvironmentUsage
                {
                    Id = environmentId,
                    EndState = finalEnvironmentStateOrError.ToString(),
                    Usage = envUsageDictionary,
                    ResourceUsage = resourceUsageDetail,
                    Sku = finalState.Sku,
                };
                currentBillSummary.UsageDetail.Add(envUsageDetail);
                MergeUsageWithEnvUsage(envUsageDictionary, currentBillSummary.Usage);
                LogEnvironmentUsageDetails(endTime, currentBillSummary.Plan.Location, envUsageDetail, childLogger.NewChildLogger());
            }
        }

        private void CalculateResourceUsageByTimeAndSku(BillingWindowSlice slice, ResourceUsageDetail usageDetail)
        {
            var correctedStartTime = slice.StartTime;
            if (correctedStartTime < DateTime.UtcNow.AddHours(BillingWindowMaximumTime))
            {
                correctedStartTime = DateTime.UtcNow.AddHours(BillingWindowMaximumTime);
            }

            var timeSpentInSlice = slice.EndTime - correctedStartTime;
            if (timeSpentInSlice.TotalSeconds >= 0)
            {
                if (slice.OverrideState == BillingOverrideState.BillingEnabled)
                {
                    if (slice.BillingState == BillingWindowBillingState.Active)
                    {
                        AddComputeTimeForSku(usageDetail, slice.Sku, timeSpentInSlice);
                    }

                    AddStorageTimeForSku(usageDetail, slice.Sku, timeSpentInSlice);
                }
            }
        }

        private void AddStorageTimeForSku(ResourceUsageDetail usageDetail, Sku sku, TimeSpan timeSpentInSlice)
        {
            if (usageDetail.Storage == null)
            {
                usageDetail.Storage = new List<StorageUsageDetail>();
            }

            if (skuDictionary.TryGetValue(sku.Name, out var skuInfo))
            {
                var skuStorage = usageDetail.Storage.FirstOrDefault(x => x.Sku == sku.Name);
                if (skuStorage == null)
                {
                    var detail = new StorageUsageDetail()
                    {
                        Sku = sku.Name,
                        Usage = timeSpentInSlice.TotalSeconds,
                        Size = skuInfo.StorageSizeInGB,
                    };
                    usageDetail.Storage.Add(detail);
                }
                else
                {
                    skuStorage.Usage += timeSpentInSlice.TotalSeconds;
                }
            }
        }

        private void AddComputeTimeForSku(ResourceUsageDetail usageDetail, Sku sku, TimeSpan timeSpentInSlice)
        {
            if (usageDetail.Compute == null)
            {
                usageDetail.Compute = new List<ComputeUsageDetail>();
            }

            var skuStorage = usageDetail.Compute.FirstOrDefault(x => x.Sku == sku.Name);
            if (skuStorage == null)
            {
                var detail = new ComputeUsageDetail()
                {
                    Sku = sku.Name,
                    Usage = timeSpentInSlice.TotalSeconds,
                };
                usageDetail.Compute.Add(detail);
            }
            else
            {
                skuStorage.Usage += timeSpentInSlice.TotalSeconds;
            }
        }

        private void LogEnvironmentUsageDetails(DateTime end, AzureLocation region, EnvironmentUsage usageDetail, IDiagnosticsLogger logger)
        {
            if (usageDetail.ResourceUsage.Compute.Count > 0)
            {
                logger.FluentAddValue("BillableComputeSeconds", usageDetail.ResourceUsage.Compute.Sum(x => x.Usage).ToString());
            }

            if (usageDetail.ResourceUsage.Storage.Count > 0)
            {
                logger.FluentAddValue("BillableStorageSeconds", usageDetail.ResourceUsage.Storage.Sum(x => x.Usage).ToString());
            }

            logger.FluentAddValue("EnvironmentSku", usageDetail.Sku.Name.ToString())
                  .FluentAddValue("Location", region.ToString())
                  .FluentAddValue("CloudEnvironmentId", usageDetail.Id)
                  .FluentAddValue("BillEndingTime", end.ToString())
                  .LogInfo("billing_summary_aggregate_environment_summary");
        }

        private void MergeUsageWithEnvUsage(IDictionary<string, double> envUsageDictionary, IDictionary<string, double> usage)
        {
            foreach (var envUsage in envUsageDictionary)
            {
                if (usage.ContainsKey(envUsage.Key))
                {
                    usage[envUsage.Key] += envUsage.Value;
                }
                else
                {
                    usage.Add(envUsage.Key, envUsage.Value);
                }
            }
        }

        private (bool isError, CloudEnvironmentState errorState) CheckForErrors(string id, CloudEnvironmentState expectedState, IEnumerable<EnvironmentStateChange> allEventsInWindow, IDiagnosticsLogger logger)
        {
            // We're deleted already, we can trust that's a terminal state
            if (expectedState == CloudEnvironmentState.Deleted)
            {
                return (false, expectedState);
            }

            var envEventsSinceStart = allEventsInWindow.Where(x => x.Environment.Id == id);

            if (envEventsSinceStart.Any())
            {
                var lastState = BillingUtilities.GetLastBillableEventStateChange(envEventsSinceStart);
                if (lastState is null)
                {
                    // We didn't have a good billable state to compare against that occured in this window.
                    return (false, expectedState);
                }

                var lastBillingStateChange = ParseEnvironmentState(lastState.NewValue);
                return (lastBillingStateChange != expectedState, lastBillingStateChange);
            }

            return (false, expectedState);
        }

        private (IEnumerable<BillingWindowSlice> Slices, BillingWindowSlice.NextState NextState) GenerateWindowSlices(DateTime end, BillingWindowSlice.NextState currState, EnvironmentStateChange evnt, IEnumerable<BillingPlanSummaryOverrideJobPayload> overrides)
        {
            var (currSlice, nextState) = CalculateNextWindow(evnt, currState, end);

            if (currSlice is null)
            {
                // there are no state machine transitions here so just bail out
                return (Enumerable.Empty<BillingWindowSlice>(), currState);
            }

            var slicesWithOverrides = GenerateSlicesWithOverrides(currSlice, overrides);
            return (slicesWithOverrides, nextState);
        }

        private IEnumerable<BillingWindowSlice> GenerateSlicesWithOverrides(BillingWindowSlice currSlice, IEnumerable<BillingPlanSummaryOverrideJobPayload> overrides)
        {
            var dividedSlices = GenerateHourBoundTimeSlices(currSlice);
            foreach (var slice in dividedSlices)
            {
                var currBillingOverride = overrides.Where(x => x.StartTime <= currSlice.StartTime && x.EndTime >= currSlice.EndTime).OrderBy(x => x.Priority).FirstOrDefault();
                if (currBillingOverride != null)
                {
                    slice.OverrideState = currBillingOverride.BillingOverrideState;
                }
            }

            return dividedSlices;
        }

        private IEnumerable<BillingWindowSlice> GenerateHourBoundTimeSlices(BillingWindowSlice currSlice)
        {
            var slices = new List<BillingWindowSlice>();
            var nextHourBoundary = new DateTime(currSlice.StartTime.Year, currSlice.StartTime.Month, currSlice.StartTime.Day, currSlice.StartTime.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            if (currSlice.EndTime <= nextHourBoundary)
            {
                slices.Add(currSlice);
            }
            else
            {
                var endTime = nextHourBoundary;
                var dividedFully = false;

                // create the input time slice and add it to the hour boundary.
                var lastSlice = new BillingWindowSlice()
                {
                    StartTime = currSlice.StartTime,
                    EndTime = endTime,
                    BillingState = currSlice.BillingState,
                    Sku = currSlice.Sku,
                };
                slices.Add(lastSlice);

                while (!dividedFully)
                {
                    endTime = endTime.AddHours(1);
                    dividedFully = true;
                    var tempEndTime = currSlice.EndTime;
                    if (currSlice.EndTime > endTime)
                    {
                        // We need to loop again and add a new slice for this hour segment since this segment is a subset.
                        tempEndTime = endTime;
                        dividedFully = false;
                    }

                    lastSlice = new BillingWindowSlice()
                    {
                        StartTime = lastSlice.EndTime,
                        EndTime = tempEndTime,
                        BillingState = currSlice.BillingState,
                        Sku = currSlice.Sku,
                    };
                    slices.Add(lastSlice);
                }
            }

            return slices;
        }

        /// <summary>
        /// Helper method to extract state transition settings from a given BillingEvent.
        /// </summary>
        /// <param name="billingEvent">The billing event.</param>
        /// <returns>A NextState which contains state transition metadata from the event.</returns>
        private BillingWindowSlice.NextState BuildNextStateFromEvent(EnvironmentStateChange billingEvent)
        {
            return new BillingWindowSlice.NextState
            {
                EnvironmentState = ParseEnvironmentState(billingEvent.NewValue),
                Sku = billingEvent.Environment.Sku,
                TransitionTime = billingEvent.Time,
            };
        }

        /// <summary>
        /// This method creates the class BillingWindowSlice<see cref="BillingWindowSlice"/>.
        /// A BillingWindowSlice represents a timespan of billing activity from one
        /// CloudEnvironmentState<see cref="CloudEnvironmentState"/> to the next. It contains all information
        /// neccessary to create a unit of billing for its period.
        /// </summary>
        /// <param name="currentEvent">The current event being processed.</param>
        /// <param name="currentState">the current state (sku, time) for the state machine.</param>
        /// <param name="endTimeForPeriod">the overall end time for the period.</param>
        /// <returns>A tuple representing the next slice/state.</returns>
        private (BillingWindowSlice Slice, BillingWindowSlice.NextState NextState) CalculateNextWindow(EnvironmentStateChange currentEvent, BillingWindowSlice.NextState currentState, DateTime endTimeForPeriod)
        {
            // If we're looking at a null current Event and the last event status is not Deleted,
            // just calculate the time delta otherwise, run through the state machine again.
            if (currentEvent == null)
            {
                // No new time slice to create if last status was Deleted.
                // There will be no more events after a Deleted event.
                if (currentState.EnvironmentState == CloudEnvironmentState.Available
                    || currentState.EnvironmentState == CloudEnvironmentState.Shutdown
                    || currentState.EnvironmentState == CloudEnvironmentState.Archived)
                {
                    var billingState = BillingWindowBillingState.Active;

                    if (currentState.EnvironmentState == CloudEnvironmentState.Shutdown)
                    {
                        billingState = BillingWindowBillingState.Inactive;
                    }
                    else if (currentState.EnvironmentState == CloudEnvironmentState.Archived)
                    {
                        billingState = BillingWindowBillingState.Archived;
                    }

                    var finalSlice = new BillingWindowSlice()
                    {
                        StartTime = currentState.TransitionTime,
                        EndTime = endTimeForPeriod,
                        BillingState = billingState,
                        Sku = currentState.Sku,
                    };

                    // currentState is also the next state here because it is still active as we have no more transition events
                    return (finalSlice, currentState);
                }
                else
                {
                    return (null, null);
                }
            }

            var nextState = BuildNextStateFromEvent(currentEvent);

            BillingWindowSlice nextSlice = null;
            switch (currentState.EnvironmentState)
            {
                case CloudEnvironmentState.Available:
                    switch (nextState.EnvironmentState)
                    {
                        // CloudEnvironmentState has gone from Available to Shutdown or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Shutdown:
                        case CloudEnvironmentState.Moved:
                        case CloudEnvironmentState.Deleted:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = currentState.TransitionTime,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Active,
                                Sku = currentState.Sku,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                case CloudEnvironmentState.Shutdown:
                    switch (nextState.EnvironmentState)
                    {
                        // CloudEnvironmentState has gone from Shutdown to Available, Archived, or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Moved:
                        case CloudEnvironmentState.Archived:
                        case CloudEnvironmentState.Deleted:
                        // Environment's state hasn't changed, but some other setting was updated so we still need to create a slice
                        case CloudEnvironmentState.Shutdown:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = currentState.TransitionTime,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Inactive,
                                Sku = currentState.Sku,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                case CloudEnvironmentState.Archived:
                    switch (nextState.EnvironmentState)
                    {
                        // CloudEnvironmentState has gone from Archived to Available, Shutdown, or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Moved:
                        case CloudEnvironmentState.Deleted:
                        case CloudEnvironmentState.Shutdown:
                        case CloudEnvironmentState.Archived:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = currentState.TransitionTime,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Archived,
                                Sku = currentState.Sku,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                // No previous billing summary
                case CloudEnvironmentState.None:
                    switch (nextState.EnvironmentState)
                    {
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Archived:
                        case CloudEnvironmentState.Shutdown:
                            nextSlice = new BillingWindowSlice
                            {
                                // Setting StartTime = EndTime because this time slice should
                                // not be billed. This represent the slice of time from
                                // Provisioning to Available, in which we won't bill.
                                // The next time slice will bill for Available time.
                                StartTime = currentEvent.Time,
                                EndTime = currentEvent.Time,
                                BillingState = BillingWindowBillingState.Inactive,
                                Sku = currentState.Sku,
                            };
                            break;
                    }

                    break;
                default:
                    break;
            }

            return (nextSlice, nextState);
        }

        /// <summary>
        /// Helper method to parse a cloud environment state from its string representation.
        /// </summary>
        /// <param name="state">The string representing the state being parsed.</param>
        /// <returns>The parsed state enum value.</returns>
        private CloudEnvironmentState ParseEnvironmentState(string state)
        {
            return (CloudEnvironmentState)Enum.Parse(typeof(CloudEnvironmentState), state);
        }

        private async Task<int> TransmitBillSummaryToPushAgent(BillingSummaryRequest billingSummaryRequest, BillSummary billingSummary, VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            if (!billingSummaryRequest.EnablePushAgentSubmission)
            {
                // bail out early
                return 0;
            }

            string batchId = billingSummary.Id;
            var numberOfSubmissions = 0;
            var storageClient = await BillingStorageFactory.CreateBillingSubmissionCloudStorage(plan.Location);
            await logger.OperationScopeAsync(
             $"billSummaryGenerator_processSummary",
             async (childLogger) =>
             {
                 foreach (var meter in billingSummary.Usage.Keys)
                 {
                     var eventID = Guid.NewGuid();
                     var quantity = billingSummary.Usage[meter];
                     if (quantity > 0)
                     {
                         // One submission per meter.
                         var billingSummaryTableSubmission = new BillingSummaryTableSubmission(batchId, eventID.ToString())
                         {
                             EventDateTime = billingSummary.PeriodEnd,
                             EventId = eventID,
                             Location = MapPav2Location(plan.Location),
                             MeterID = meter,
                             SubscriptionId = plan.Subscription,
                             ResourceUri = plan.ResourceId,
                             Quantity = billingSummary.Usage[meter],
                         };

                         // Submit all the stuff. An entry needs to be added to the table with the usage record
                         await storageClient.InsertOrUpdateBillingTableSubmission(billingSummaryTableSubmission);
                         numberOfSubmissions++;
                         childLogger.FluentAddValue("Quantity", quantity.ToString())
                               .FluentAddValue("BillSummaryEndTime", billingSummary.PeriodEnd.ToString())
                               .FluentAddValue("ResourceId", plan.ResourceId)
                               .FluentAddValue("Subscription", plan.Subscription)
                               .FluentAddValue("Meter", meter)
                               .LogInfo("billSummarySubmission");
                     }
                 }

                 var billingSummaryQueueSubmission = new BillingSummaryQueueSubmission()
                 {
                     BatchId = batchId,
                     PartitionKey = batchId,
                 };

                 // Send off the queue submission
                 await storageClient.PushBillingQueueSubmission(billingSummaryQueueSubmission);

                 // log some telemtry
                 childLogger.FluentAddValue("BillSubmissionEndTime", billingSummary.PeriodEnd.ToString())
                   .FluentAddValue("SubmissionCount", numberOfSubmissions.ToString())
                   .LogInfo("billing_aggregate_shard_submission");
             });
            return numberOfSubmissions;
        }

        private string MapPav2Location(AzureLocation location)
        {
            // TODO : Match all the other locations since PAV2 locations have slightly different naming
            switch (location)
            {
                case AzureLocation.EastUs:
                    return "useast";
                case AzureLocation.SouthEastAsia:
                    return "asiasoutheast";
                case AzureLocation.WestEurope:
                    return "europewest";
                case AzureLocation.WestUs:
                    return "uswest";
                case AzureLocation.WestUs2:
                    return "uswest2";
                default:
                    return null;
            }
        }
    }
}
