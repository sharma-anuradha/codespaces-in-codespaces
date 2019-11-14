// <copyright file="BillingService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using UsageDictionary = System.Collections.Generic.Dictionary<string, double>;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Calculates billing units per VSO SkuPlan in the current control plane region(s)
    /// and saves a BillingSummary to the environment_billing_events table.
    /// </summary>
    public partial class BillingService : BillingServiceBase, IBillingService
    {
        private readonly IBillingEventManager billingEventManager;
        private readonly ISkuCatalog skuCatalog;
        private readonly string billingSummaryType = BillingEventTypes.BillingSummary;
        private readonly string DeletedEnvState = nameof(CloudEnvironmentState.Deleted);
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> SkuDictionary;
        // TODO: move the meter Dictionary to AppSettings
        private readonly IDictionary<AzureLocation, string> meterDictionary = new Dictionary<AzureLocation, string>
        {
            {AzureLocation.EastUs, "91f658f0-9ce6-4f5d-8795-aa224cb83ccc" },
            {AzureLocation.WestUs2, "5f3afa79-01ad-4d7e-b691-73feca4ea350" },
            {AzureLocation.WestEurope, "edd2e9c5-56ce-469f-aedb-ba82f4e745cd" },
            {AzureLocation.SouthEastAsia, "12e4ab51-ee20-4bbc-95a6-9dddea31e634" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingService"/> class.
        /// </summary>
        /// <param name="billingEventManager"></param>
        /// <param name="controlPlaneInfo"></param>
        public BillingService(IBillingEventManager billingEventManager,
                            IControlPlaneInfo controlPlaneInfo,
                            ISkuCatalog skuCatalog,
                            IDiagnosticsLogger diagnosticsLogger,
                            IClaimedDistributedLease claimedDistributedLease,
                            ITaskHelper taskHelper)
            : base(billingEventManager, controlPlaneInfo, diagnosticsLogger, claimedDistributedLease, taskHelper, "billingworker")
        {
            Requires.NotNull(billingEventManager, nameof(billingEventManager));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(skuCatalog, nameof(skuCatalog));
            Requires.NotNull(diagnosticsLogger, nameof(diagnosticsLogger));
            Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));

            this.billingEventManager = billingEventManager;
            this.skuCatalog = skuCatalog;
            SkuDictionary = this.skuCatalog.CloudEnvironmentSkus;

            // Send all logs to billingservices Geneva logger table
            Logger.FluentAddBaseValue("Service", "billingservices");
        }

        /// <inheritdoc/>
        public async Task GenerateBillingSummaryAsync(CancellationToken cancellationToken)
        {
            await Execute(cancellationToken);
        }

        protected override async Task ExecuteInner(IDiagnosticsLogger childlogger, DateTime start, DateTime end, string planShard, AzureLocation region)
        {
            var plans = await billingEventManager.GetPlansByShardAsync(
                                                start,
                                                end,
                                                childlogger,
                                                new List<AzureLocation> { region },
                                                planShard);
            foreach (var plan in plans)
            {
                await BeginAccountCalculations(plan, start, end, childlogger);
            }
        }

        /// <summary>
        /// This method controls the main logic flow of generating BillingSummary records.
        /// The last BillingSummary is fetched and used as the seed event for the billing unit
        /// calculations on the current events.
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="start"></param>
        /// <param name="desiredBillEndTime"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async Task BeginAccountCalculations(VsoPlanInfo plan, DateTime start, DateTime desiredBillEndTime, IDiagnosticsLogger logger)
        {
            logger.AddVsoPlan(plan)
                .FluentAddBaseValue("startCalculationTime", start)
                .FluentAddBaseValue("endCalculationTime", desiredBillEndTime);

            await logger.OperationScopeAsync(
                $"{ServiceName}_begin_plan_calculations",
                async (childLogger) =>
                {
                    var currentTime = DateTime.UtcNow;

                    // Get the last BillingSummary based on the current time.
                    var summaryEvents = await billingEventManager.GetPlanEventsAsync(plan, start, currentTime, new string[] { billingSummaryType }, logger);
                    BillingEvent latestBillingEventSummary;
                    if (!summaryEvents.Any())
                    {
                        // No summary events exist for this plan eg. first run of the BillingWorker
                        // Create a generic BillingSummary to use as a starting point for the following
                        // billing calculations, and seed it with this billing period's start time.
                        latestBillingEventSummary = new BillingEvent
                        {
                            Time = start,
                            Args = new BillingSummary
                            {
                                PeriodEnd = start,
                            },
                        };
                    }
                    else
                    {
                        latestBillingEventSummary = summaryEvents.Last();

                        // Check if the last summary's PeriodEnd matches the end time we are trying to bill for.
                        if (((BillingSummary)latestBillingEventSummary.Args).PeriodEnd >= desiredBillEndTime)
                        {
                            // there is no work to do since the last summary we found matches the time we were going to generate a new summary for.
                            return;
                        }
                    }

                    var latestBillingSummary = (BillingSummary)latestBillingEventSummary.Args;

                    // Get EnvironmentStateChnage events for the given plan during the given period.
                    var billingEvents = await billingEventManager.GetPlanEventsAsync(plan, latestBillingSummary.PeriodEnd, desiredBillEndTime, new string[] { BillingEventTypes.EnvironmentStateChange }, logger);

                    // Using the above EnvironmentStateChange events and the previous BillingSummary create the current BillingSummary.
                    var billingSummary = await CalculateBillingUnits(plan, billingEvents, (BillingSummary)latestBillingEventSummary.Args, desiredBillEndTime);

                    // Append to the current BillingSummary any environments that did not have billing events during this period, but were present in the previous BillingSummary.
                    var totalBillingSummary = await CaculateBillingForEnvironmentsWithNoEvents(plan, billingSummary, latestBillingEventSummary, desiredBillEndTime);
                    await billingEventManager.CreateEventAsync(plan, null, billingSummaryType, totalBillingSummary, logger);
                },
                swallowException: true);

            // TODO: Need to handle subscriptionState events (No billing for suspended subscriptions)
        }

        /// <summary>
        /// This method loops through all the BillingEvents<see cref="BillingEvent"/> in this current billing period and generates a
        /// BillingSummary. The last BillingSummary is used as the starting point for billing unit calculations.
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="events"></param>
        /// <param name="startSummary"></param>
        /// <param name="end"></param>
        /// <returns></returns>

        public async Task<BillingSummary> CalculateBillingUnits(

            VsoPlanInfo plan,
            IEnumerable<BillingEvent> events,
            BillingSummary startSummary,
            DateTime end)
        {
            var totalBillable = 0.0d;
            var environmentUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();
            var userUsageDetails = new Dictionary<string, UserUsageDetail>();
            var meterId = GetMeterIdByAzureLocation(plan.Location);

            // Get events per environment
            var billingEventsPerEnvironment = events.GroupBy(b => b.Environment.Id);
            foreach (var environmentEvents in billingEventsPerEnvironment)
            {
                var billable = 0.0d;
                BillingEvent current = null;
                var seqEvents = environmentEvents.OrderBy(t => t.Time);
                var environmentDetails = environmentEvents.First().Environment;
                List<BillingWindowSlice> slices = new List<BillingWindowSlice>();

                // Create an initial BillingWindowSlice to use as a starting point for billing calculations
                // The previous BillingSummary is used as the StartTime and EndTime. The environments
                // previous state comes from the list of environment details in the previous BillingSummary.

                string endPreviousState;
                if (startSummary?.UsageDetail?.Environments != null && startSummary.UsageDetail.Environments.ContainsKey(environmentEvents.Key))
                {
                    endPreviousState = startSummary.UsageDetail?.Environments[environmentEvents.Key].EndState;
                }
                else
                {
                    endPreviousState = CloudEnvironmentState.None.ToString();
                }

                var initialSlice = new BillingWindowSlice()
                {
                    BillingState = BillingWindowBillingState.Active,
                    StartTime = startSummary.PeriodEnd,
                    EndTime = startSummary.PeriodEnd,
                    LastState = ParseEnvironmentState(endPreviousState),
                };

                var currSlice = initialSlice;
                IEnumerable<BillingWindowSlice> allSlices;

                // Loop through each billing event for the current environment.
                foreach (var evnt in seqEvents)
                {
                    allSlices = await GenerateWindowSlices(end, currSlice, evnt, plan, environmentDetails.Sku);
                    if (allSlices.Any())
                    {
                        slices.AddRange(allSlices);
                        currSlice = allSlices.Last();
                    }
                }

                // Get the remainder or the entire window if there were no events.
                allSlices = await GenerateWindowSlices(end, currSlice, null, plan, environmentDetails.Sku);
                if (allSlices.Any())
                {
                    slices.AddRange(allSlices);
                    currSlice = allSlices.Last();
                }

                // We might not have any billable slices. If we don't, let's not do anything
                if (slices.Any())
                {
                    var containsErrors = await CheckForErrors(plan, end, environmentDetails.Id, slices.Last().LastState);
                    EnvironmentUsageDetail usageDetail;
                    if (!containsErrors.isError)
                    {
                        // Aggregate the billable units for each slice created above.
                        slices.ForEach((slice) => billable += CalculateVsoUnitsByTimeAndSku(slice, environmentDetails.Sku.Name));
                        usageDetail = new EnvironmentUsageDetail
                        {
                            Name = environmentDetails.Name,
                            EndState = slices.Last().LastState.ToString(),
                            Usage = new UsageDictionary
                            {
                                { meterId, billable },
                            },
                            Sku = environmentDetails.Sku,
                            UserId = environmentDetails.UserId,
                        };
                    }
                    else
                    {
                        usageDetail = new EnvironmentUsageDetail
                        {
                            Name = environmentDetails.Name,
                            EndState = containsErrors.errorState.ToString(),
                            Usage = new UsageDictionary
                            {
                                { meterId, billable },
                            },
                            Sku = environmentDetails.Sku,
                            UserId = environmentDetails.UserId,
                        };

                        Logger.AddValue("SubscriptionId", plan.Subscription);
                        Logger.AddValue("ResourceId", plan.ResourceId);
                        Logger.LogError("billingworker_plan_correction");
                    }
                    environmentUsageDetails.Add(environmentEvents.Key, usageDetail);
                    userUsageDetails = AggregateUserUsageDetails(userUsageDetails, billable, environmentDetails.UserId, meterId);
                    totalBillable += billable;
                }
            }

            return new BillingSummary
            {
                PeriodStart = startSummary.PeriodEnd,
                PeriodEnd = end,
                UsageDetail = new UsageDetail { Environments = environmentUsageDetails, Users = userUsageDetails, },
                SubscriptionState = string.Empty,
                Plan = string.Empty,
                SubmissionState = BillingSubmissionState.None,
                Usage = new UsageDictionary
                {
                    { meterId, totalBillable },
                },
            };
        }

        private async Task<(bool isError, CloudEnvironmentState errorState)> CheckForErrors(VsoPlanInfo plan, DateTime end, string id, CloudEnvironmentState expectedState)
        {
            // We're deleted already, we can trust that's a terminal state
            if (expectedState == CloudEnvironmentState.Deleted)
            {
                return (false, expectedState);
            }
            // We need to check if the last state matches the expected billing state. 
            // Look back and see if our last billing state matches what we expect.
            // If it is different, return an error and the expected state.
            // Otherwise, we should continue with our expected state.
            Expression<Func<BillingEvent, bool>> filter = bev => bev.Plan == plan &&
                                                   bev.Time < end &&
                                                   bev.Environment.Id == id &&
                                                   bev.Type == BillingEventTypes.EnvironmentStateChange;

            var envEventsSinceStart = await billingEventManager.GetPlanEventsAsync(filter, Logger);
            if (envEventsSinceStart.Any())
            {
                var lastState = GetLastBillableEventStateChange(envEventsSinceStart);
                if(lastState is null)
                {
                    // If we get this far, we could not find an appropriate billing event
                    Logger.AddValue("cloudEnvironmentId", id);
                    Logger.LogError("billingworker_missing_events_for_environments");
                    return (true, CloudEnvironmentState.Deleted);
                }
                var lastBillingStateChange = ParseEnvironmentState((lastState.Args as BillingStateChange).NewValue);
                return (lastBillingStateChange != expectedState, lastBillingStateChange);
            }

            // If we get this far, our we did not get any billing events. We should no longer be billing the customer and should investigate
            Logger.AddValue("cloudEnvironmentId", id);
            Logger.LogError("billingworker_missing_events_for_environments");
            return (true, CloudEnvironmentState.Deleted);
        }

        private BillingEvent GetLastBillableEventStateChange(IEnumerable<BillingEvent> envEventsSinceStart)
        {
            BillingEvent lastState = null;
            foreach (var billEvent in envEventsSinceStart)
            {
                var billingStateChange = (BillingStateChange)billEvent.Args;
                var newValue = billingStateChange.NewValue;
                if (newValue.Equals(CloudEnvironmentState.Available.ToString()) ||
                    newValue.Equals(CloudEnvironmentState.Shutdown.ToString()) ||
                    newValue.Equals(CloudEnvironmentState.Deleted.ToString()))
                {
                    lastState = billEvent;
                }
            }

            return lastState;
        }

        private async Task<IEnumerable<BillingWindowSlice>> GenerateWindowSlices(DateTime end, BillingWindowSlice currSlice, BillingEvent evnt, VsoPlanInfo plan, Sku sku)
        {
            currSlice = CalculateNextWindow(evnt, currSlice, end);

            if (currSlice is null)
            {
                // there are no state machine transitions here so just bail out
                return Enumerable.Empty<BillingWindowSlice>();
            }

            var slicesWithOverrides = await GenerateSlicesWithOverrides(currSlice, plan, sku);
            return slicesWithOverrides;
        }

        private async Task<IEnumerable<BillingWindowSlice>> GenerateSlicesWithOverrides(BillingWindowSlice currSlice, VsoPlanInfo plan, Sku sku)
        {
            var dividedSlices = GenerateHourBoundTimeSlices(currSlice);
            foreach (var slice in dividedSlices)
            {
                var currBillingOverride = await billingEventManager.GetOverrideStateForTimeAsync(slice.StartTime, plan.Subscription, plan, sku, Logger);
                if (currBillingOverride != null)
                {
                    slice.OverrideState = currBillingOverride.BillingOverrideState;
                }
            }
            return dividedSlices;
        }

        private IEnumerable<BillingWindowSlice> GenerateHourBoundTimeSlices(BillingWindowSlice currSlice)
        {

            List<BillingWindowSlice> slices = new List<BillingWindowSlice>();
            DateTime nextHourBoundary = new DateTime(currSlice.StartTime.Year, currSlice.StartTime.Month, currSlice.StartTime.Day, currSlice.StartTime.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
            if (currSlice.EndTime <= nextHourBoundary)
            {
                slices.Add(currSlice);
            }
            else
            {
                var startTime = currSlice.StartTime;
                var endTime = nextHourBoundary;
                bool dividedFully = false;

                // create the input time slice and add it to the hour boundary.
                var lastSlice = new BillingWindowSlice()
                {
                    StartTime = currSlice.StartTime,
                    EndTime = endTime,
                    BillingState = currSlice.BillingState,
                    LastState = currSlice.LastState,
                };
                slices.Add(lastSlice);

                while (!dividedFully)
                {
                    endTime = endTime.AddHours(1);
                    if (currSlice.EndTime > endTime)
                    {
                        // We need to loop again and add a new slice for this hour segment since this segment is a subset.
                        lastSlice = new BillingWindowSlice()
                        {
                            StartTime = lastSlice.EndTime,
                            EndTime = endTime,
                            BillingState = currSlice.BillingState,
                            LastState = currSlice.LastState,
                        };
                        slices.Add(lastSlice);
                    }
                    else
                    {
                        // Get the remainder of the time
                        dividedFully = true;
                        lastSlice = new BillingWindowSlice()
                        {
                            StartTime = lastSlice.EndTime,
                            EndTime = currSlice.EndTime,
                            BillingState = currSlice.BillingState,
                            LastState = currSlice.LastState,
                        };
                        slices.Add(lastSlice);
                    }
                }
            }
            return slices;
        }

        /// <summary>
        /// This method creates the class BillingWindowSlice<see cref="BillingWindowSlice"/>.
        /// A BillingWindowSlice represents a timespan of billing activity from one
        /// CloudEnvironmentState<see cref="CloudEnvironmentState"/> to the next. It contains all information
        /// neccessary to create a unit of billing for its period.
        /// </summary>
        /// <param name="currentEvent"></param>
        /// <param name="lastSlice"></param>
        /// <param name="endTimeForPeriod"></param>
        /// <returns></returns>
        private BillingWindowSlice CalculateNextWindow(BillingEvent currentEvent, BillingWindowSlice lastSlice, DateTime endTimeForPeriod)
        {
            // If we're looking at a null current Event and the last event status is not Deleted,
            // just calculate the time delta otherwise, run through the state machine again.

            if (currentEvent == null)
            {
                // No new time slice to create if last status was Deleted.
                // There will be no more events after a Deleted event.
                if (lastSlice.LastState == CloudEnvironmentState.Available || lastSlice.LastState == CloudEnvironmentState.Shutdown)
                {
                    var finalSlice = new BillingWindowSlice()
                    {
                        BillingState = lastSlice.LastState == CloudEnvironmentState.Shutdown ?
                        BillingWindowBillingState.Inactive : BillingWindowBillingState.Active,
                        EndTime = endTimeForPeriod,
                        StartTime = lastSlice.EndTime,
                        LastState = lastSlice.LastState,
                    };
                    return finalSlice;
                }
                else
                {
                    return null;
                }


            }

            var currentState = lastSlice.LastState;
            var nextState = ParseEnvironmentState(((BillingStateChange)currentEvent.Args).NewValue);
            BillingWindowSlice nextSlice = null;
            switch (currentState)
            {
                case CloudEnvironmentState.Available:
                    switch (nextState)
                    {
                        // CloudEnvironmentState has gone from Available to Shutdown or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Shutdown:
                        case CloudEnvironmentState.Deleted:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = lastSlice.EndTime,
                                EndTime = currentEvent.Time,
                                LastState = nextState,
                                BillingState = BillingWindowBillingState.Active,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                case CloudEnvironmentState.Shutdown:
                    switch (nextState)
                    {
                        // CloudEnvironmentState has gone from Shutdown to Available or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Deleted:
                            nextSlice = new BillingWindowSlice()
                            {
                                StartTime = lastSlice.EndTime,
                                EndTime = currentEvent.Time,
                                LastState = nextState,
                                BillingState = BillingWindowBillingState.Inactive,
                            };
                            break;
                        default:
                            break;
                    }

                    break;

                // No previous billing summary
                case CloudEnvironmentState.None:
                    switch (nextState)
                    {
                        case CloudEnvironmentState.Available:
                            nextSlice = new BillingWindowSlice
                            {
                                // Setting StartTime = EndTime because this time slice should
                                // not be billed. This represent the slice of time from
                                // Provisioning to Available, in which we won't bill.
                                // The next time slice will bill for Available time.
                                StartTime = currentEvent.Time,
                                EndTime = currentEvent.Time,
                                LastState = nextState,
                                BillingState = BillingWindowBillingState.Inactive,
                            };
                            break;
                    }

                    break;
                default:
                    break;
            }

            return nextSlice;
        }

        /// <summary>
        /// This method will generate a BillingSummary that includes environments that did not have any billing events
        /// during the given period. The previous BillingSummary and the current BillingSummary are compared to determine
        /// if any environments exist in the previous but not in the current. For those environments a billing unit is
        /// calculated and appended to the current BillingSummary.
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="currentSummary"></param>
        /// <param name="latestEvent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public async Task<BillingSummary> CaculateBillingForEnvironmentsWithNoEvents(

            VsoPlanInfo plan,
            BillingSummary currentSummary,
            BillingEvent latestEvent,
            DateTime end)
        {
            // Scenario: Environment has no billing Events in this billing period
            var lastSummary = (BillingSummary)latestEvent.Args;
            if (lastSummary == null || lastSummary.UsageDetail == null)
            {
                // TODO: design for no billing summary in this time period
                // We may want to widen our summary search
                // No previous summary environments
                Logger.AddVsoPlan(plan);
                Logger.LogWarning("No BillingSummary found in the last 48 hours for the plan.");
                return currentSummary;
            }

            IDictionary<string, EnvironmentUsageDetail> currentPeriodEnvironments = new Dictionary<string, EnvironmentUsageDetail>();
            if (currentSummary != null && currentSummary.UsageDetail != null)
            {
                currentPeriodEnvironments = currentSummary.UsageDetail.Environments;
            }
            else
            {
                // Create a BillingSummary if no summary exists for this billing period.
                // This mean no billing events were logged and we only need to process
                // environments whose EndState was Available or Shutdown in the previous BillingSummary.
                currentSummary = new BillingSummary
                {
                    PeriodStart = lastSummary.PeriodEnd,
                    PeriodEnd = end,
                    UsageDetail = new UsageDetail
                    {
                        Environments = new Dictionary<string, EnvironmentUsageDetail>(),
                        Users = new Dictionary<string, UserUsageDetail>(),
                    },
                    SubscriptionState = string.Empty,
                    Plan = string.Empty,
                    SubmissionState = BillingSubmissionState.None,
                    Usage = new UsageDictionary(),
                };
            }

            // Find environments that are present in the previous BillingSummary and not present in the current.
            var lastPeriodEnvmts = lastSummary.UsageDetail.Environments;

            // Get a list of environment Ids present in the last billing summary but not in the current summary.
            var missingEnvmtIds = lastPeriodEnvmts.Keys.Where(i => !currentPeriodEnvironments.ContainsKey(i)).ToList();
            if (!missingEnvmtIds.Any())
            {
                // No missing environment Ids found.
                return currentSummary;
            }

            var meterId = GetMeterIdByAzureLocation(plan.Location);
            var envUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();

            // Loop through the missing environments and calculate billing.
            var missingEnvironments = lastPeriodEnvmts.Where(i => missingEnvmtIds.Contains(i.Key)).ToList();
            foreach (var environment in missingEnvironments)
            {
                var envUsageDetail = environment.Value;
                var endState = ParseEnvironmentState(envUsageDetail.EndState);
                if (envUsageDetail.EndState == DeletedEnvState)
                {
                    // Environment state is Deleted so nothing new to bill.
                    continue;
                }

                // Create a BillingWindowSlice that represents the previous state of this environment.
                var billingSlice = new BillingWindowSlice
                {
                    BillingState = endState == CloudEnvironmentState.Shutdown ?
                        BillingWindowBillingState.Inactive : BillingWindowBillingState.Active,
                    EndTime = lastSummary.PeriodEnd,
                    LastState = endState,
                    StartTime = lastSummary.PeriodEnd,
                };

                // Get the remainder or the entire window if there were no events.
                var allSlices = (await GenerateWindowSlices(end, billingSlice, null, plan, envUsageDetail.Sku)).ToList();
                if (allSlices.Any())
                {
                    billingSlice = allSlices.Last();
                }

                var billable = 0d;

                var containsDeletionErrors = await CheckForErrors(plan, end, environment.Key, billingSlice.LastState);
                if (!containsDeletionErrors.isError)
                {
                    allSlices.ForEach((slice) => billable += CalculateVsoUnitsByTimeAndSku(slice, envUsageDetail.Sku.Name));
                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = envUsageDetail.Name,
                        EndState = billingSlice.LastState.ToString(),
                        Usage = new UsageDictionary
                        {
                            { meterId, billable },
                        },
                        Sku = environment.Value.Sku,
                        UserId = environment.Value.UserId,
                    };
                    // Update Environment list, SkuPlan billable units, and User billable units
                    currentSummary.UsageDetail.Environments.Add(environment.Key, usageDetail);
                }
                else
                {
                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = envUsageDetail.Name,
                        EndState = containsDeletionErrors.errorState.ToString(),
                        Usage = new UsageDictionary
                        {
                            { meterId, billable },
                        },
                        Sku = environment.Value.Sku,
                        UserId = environment.Value.UserId,
                    };

                    Logger.AddValue("SubscriptionId", plan.Subscription);
                    Logger.AddValue("ResourceId", plan.ResourceId);
                    Logger.LogError("billingworker_plan_correction");

                    // Update Environment list, SkuPlan billable units, and User billable units
                    currentSummary.UsageDetail.Environments.Add(environment.Key, usageDetail);
                }
                if (currentSummary.Usage.ContainsKey(meterId))
                {
                    currentSummary.Usage[meterId] = currentSummary.Usage[meterId] + billable;
                }
                else
                {
                    currentSummary.Usage.Add(meterId, billable);
                }

                currentSummary.UsageDetail.Users = AggregateUserUsageDetails(currentSummary.UsageDetail.Users, billable, environment.Value.UserId, meterId);
            }

            return currentSummary;
        }

        /// <summary>
        /// Helper method to update or add a userId and billing unit to the current UserUsageDetails dictionary.
        /// The dictionary is a property of the BillingSummary<see cref="BillingSummary"/>.
        /// </summary>
        /// <param name="currentlist"></param>
        /// <param name="billable"></param>
        /// <param name="userId"></param>
        /// <param name="meterId"></param>
        /// <returns></returns>
        private Dictionary<string, UserUsageDetail> AggregateUserUsageDetails(
            IDictionary<string, UserUsageDetail> currentlist,
            double billable,
            string userId,
            string meterId)
        {
            if (currentlist.TryGetValue(userId, out UserUsageDetail userUsageDetail))
            {
                var oldValue = 0.0d;
                userUsageDetail.Usage.TryGetValue(meterId, out oldValue);
                userUsageDetail.Usage[meterId] = billable + oldValue;
            }
            else
            {
                currentlist.Add(userId, new UserUsageDetail
                {
                    Usage = new UsageDictionary { { meterId, billable } },
                });
            }

            return currentlist as Dictionary<string, UserUsageDetail>;
        }

        /// <summary>
        /// Helper method to perform the billing unit calculation. This method accepts the BillingWindowSlice and sku
        /// name, returning the billable unit for the given period of time.
        /// </summary>
        /// <param name="slice"></param>
        /// <param name="sku"></param>
        /// <returns></returns>
        private double CalculateVsoUnitsByTimeAndSku(BillingWindowSlice slice, string sku)
        {
            if (slice.OverrideState == BillingOverrideState.BillingEnabled)
            {
                var vsoUnitsPerHour = GetVsoUnitsBySkuName(sku, slice.BillingState);
                const decimal secondsPerHour = 3600m;
                var vsoUnitsPerSecond = vsoUnitsPerHour / secondsPerHour;
                var totalSeconds = (decimal)slice.EndTime.Subtract(slice.StartTime).TotalSeconds;
                var vsoUnits = totalSeconds * vsoUnitsPerSecond;
                return (double)vsoUnits;
            }
            else
            {
                // Billing is disabled for this time slice
                return 0;
            }
        }

        /// <summary>
        /// Helper method to return active or inactive VSO units from CloudEnvironmentSku<see cref="CloudEnvironmentSku"/> name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        private decimal GetVsoUnitsBySkuName(string name, BillingWindowBillingState billingState)
        {
            if (SkuDictionary.TryGetValue(name, out var sku))
            {
                if (billingState == BillingWindowBillingState.Active)
                {
                    return sku.GetActiveVsoUnitsPerHour();
                }
                else if (billingState == BillingWindowBillingState.Inactive)
                {
                    return sku.GetInactiveVsoUnitsPerHour();
                }
                else
                {
                    Logger.AddValue("BillingWindowState", billingState.ToString());
                    Logger.LogError("BillingWindowState is in an unsupported state.");
                    return 0;
                }
            }
            else
            {
                Logger.FluentAddBaseValue("SkuName", name);
                Logger.LogError("A Sku is being used on an environment that does not have a corresponding Sku catelog record.");
                return 0.0m;
            }
        }

        /// <summary>
        /// Helper method to find billing meter Id by AzureLocation.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private string GetMeterIdByAzureLocation(AzureLocation location)
        {
            if (meterDictionary.TryGetValue(location, out string id))
            {
                return id;
            }
            else
            {
                Logger.FluentAddBaseValue("Azurelocation", location.ToString());
                Logger.LogError("An Azure location was used that does not have a corresponding meter Id.");
                return "METER_NOT_FOUND";
            }
        }

        /// <summary>
        /// Helper method to parse a cloud environment state from its string representation.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private CloudEnvironmentState ParseEnvironmentState(string state)
        {
            return (CloudEnvironmentState)Enum.Parse(typeof(CloudEnvironmentState), state);
        }
    }
}
