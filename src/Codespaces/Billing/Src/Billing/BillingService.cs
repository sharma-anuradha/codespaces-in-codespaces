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
        private const string Compute = "compute";
        private const string Storage = "storage";

        private readonly IBillingEventManager billingEventManager;
        private readonly ISkuCatalog skuCatalog;
        private readonly IPlanManager planManager;
        private readonly string billingSummaryType = BillingEventTypes.BillingSummary;
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> skuDictionary;

        // TODO: move the meter Dictionary to AppSettings
        private readonly IDictionary<AzureLocation, string> meterDictionary = new Dictionary<AzureLocation, string>
        {
            { AzureLocation.EastUs, "91f658f0-9ce6-4f5d-8795-aa224cb83ccc" },
            { AzureLocation.WestUs2, "5f3afa79-01ad-4d7e-b691-73feca4ea350" },
            { AzureLocation.WestEurope, "edd2e9c5-56ce-469f-aedb-ba82f4e745cd" },
            { AzureLocation.SouthEastAsia, "12e4ab51-ee20-4bbc-95a6-9dddea31e634" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingService"/> class.
        /// </summary>
        /// <param name="billingEventManager">The billing manager used for queries.</param>
        /// <param name="controlPlaneInfo">Control plan info used to get azure regions this worker applies to.</param>
        /// <param name="skuCatalog">The catalog of skus used for costing.</param>
        /// <param name="diagnosticsLogger">The logger.</param>
        /// <param name="planManager">Used to get a list of plans to apply bills to.</param>
        /// <param name="claimedDistributedLease">used to get a lease.</param>
        /// <param name="taskHelper">Used to run multiple workers in parallel.</param>
        public BillingService(
            IBillingEventManager billingEventManager,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            IDiagnosticsLogger diagnosticsLogger,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IPlanManager planManager)
            : base(controlPlaneInfo, diagnosticsLogger, claimedDistributedLease, taskHelper, planManager)
        {
            Requires.NotNull(billingEventManager, nameof(billingEventManager));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(skuCatalog, nameof(skuCatalog));
            Requires.NotNull(diagnosticsLogger, nameof(diagnosticsLogger));
            Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));

            this.billingEventManager = billingEventManager;
            this.skuCatalog = skuCatalog;
            this.planManager = planManager;
            skuDictionary = this.skuCatalog.CloudEnvironmentSkus;

            // Send all logs to billingservices Geneva logger table
            Logger.FluentAddBaseValue("Service", "billingservices");
        }

        /// <inheritdoc/>
        protected override string ServiceName => "billingworker";

        /// <inheritdoc/>
        public async Task GenerateBillingSummaryAsync(CancellationToken cancellationToken)
        {
            await Execute(cancellationToken);
        }

        /// <summary>
        /// This method controls the main logic flow of generating BillingSummary records.
        /// The last BillingSummary is fetched and used as the seed event for the billing unit
        /// calculations on the current events.
        /// </summary>
        /// <param name="plan">the current plan being billed.</param>
        /// <param name="start">the start time for the billing period.</param>
        /// <param name="desiredBillEndTime">the end time for the billing period.</param>
        /// <param name="logger">the logger.</param>
        /// <param name="region">The region this bill applies to.</param>
        /// <param name="shardUsageTimes">used to store calculation on the usage for a particular shard.</param>
        /// <returns>Task indicating when the bill has been generated.</returns>
        public async Task BeginAccountCalculations(VsoPlan plan, DateTime start, DateTime desiredBillEndTime, IDiagnosticsLogger logger, AzureLocation region, Dictionary<string, double> shardUsageTimes)
        {
            logger.AddVsoPlan(plan)
                .FluentAddBaseValue("startCalculationTime", start)
                .FluentAddBaseValue("endCalculationTime", desiredBillEndTime);

            await logger.OperationScopeAsync(
                $"{ServiceName}_begin_plan_calculations",
                async (childLogger) =>
                {
                    var currentTime = DateTime.UtcNow;

                    // Get all events so we have only one call to underlying collections.
                    var allEvents = await billingEventManager.GetPlanEventsAsync(plan.Plan, start, currentTime, null, childLogger.NewChildLogger());

                    // Get all the billing summaries
                    var summaryEvents = allEvents.Where(x => x.Args is BillingSummary).OrderByDescending(x => ((BillingSummary)x.Args).PeriodEnd);
                    BillingEvent latestBillingEventSummary;
                    if (!summaryEvents.Any())
                    {
                        // No summary events exist for this plan eg. first run of the BillingWorker.
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
                        latestBillingEventSummary = summaryEvents.First();

                        // Check if the last summary's PeriodEnd matches the end time we are trying to bill for.
                        if (((BillingSummary)latestBillingEventSummary.Args).PeriodEnd >= desiredBillEndTime)
                        {
                            // there is no work to do since the last summary we found matches the time we were going to generate a new summary for.
                            return;
                        }
                    }

                    var latestBillingSummary = (BillingSummary)latestBillingEventSummary.Args;

                    // Now filter on all the environment state changes. Find all that happened within the last billing summary and the desired end time.
                    var billingEvents = allEvents.Where(x => x.Time >= latestBillingSummary.PeriodEnd && x.Time < desiredBillEndTime && x.Args is BillingStateChange);

                    // Gets all environment events. This is used for Error checking.
                    var allEnvironmentEvents = await GetAllEnvironmentStateChanges(plan.Plan, desiredBillEndTime, childLogger);

                    // Using the above EnvironmentStateChange events and the previous BillingSummary create the current BillingSummary.
                    var billingSummary = await CalculateBillingUnits(plan.Plan, billingEvents, (BillingSummary)latestBillingEventSummary.Args, desiredBillEndTime, region, shardUsageTimes, allEnvironmentEvents, childLogger);

                    // Append to the current BillingSummary any environments that did not have billing events during this period, but were present in the previous BillingSummary.
                    var totalBillingSummary = await CaculateBillingForEnvironmentsWithNoEvents(plan.Plan, billingSummary, latestBillingEventSummary, desiredBillEndTime, region, shardUsageTimes, allEnvironmentEvents, childLogger);

                    // Checks for any missing environments in this summary that should be in it.
                    CheckForMissingEnvironments(totalBillingSummary, desiredBillEndTime, allEnvironmentEvents, childLogger);

                    if (plan.IsDeleted)
                    {
                        // If the Plan has been deleted, track this state so that during the next billing iterating, we can mark it as the final bill.
                        totalBillingSummary.PlanIsDeleted = true;

                        // If the last (most recently submitted) billing summary also noted that the plan had been deleted, then this is the final bill.
                        totalBillingSummary.IsFinalBill = latestBillingSummary.PlanIsDeleted;
                    }

                    // Write out the summary as the last action. This should always be the last action.
                    await billingEventManager.CreateEventAsync(plan.Plan, null, billingSummaryType, totalBillingSummary, childLogger.NewChildLogger());
                },
                swallowException: true);

            // TODO: Need to handle subscriptionState events (No billing for suspended subscriptions)
        }

        /// <summary>
        /// This method loops through all the BillingEvents<see cref="BillingEvent"/> in this current billing period and generates a
        /// BillingSummary. The last BillingSummary is used as the starting point for billing unit calculations.
        /// </summary>
        /// <param name="plan">The plan being calculated for.</param>
        /// <param name="events">all events that are captured within this summary.</param>
        /// <param name="lastSummary">the summary to base this calculation off from.</param>
        /// <param name="billSummaryEndTime">the end time for the summary.</param>
        /// <param name="region">The region the bill is being calculated for.</param>
        /// <param name="shardUsageTimes">Aggregate dictionary for the total time being added to by this bill.</param>
        /// <param name="allEnvironmentChangesForPlan">Gets all environment state changes for a particular plan. Used for error checking.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A new billing summary for this plan based on new state changes.</returns>
        public async Task<BillingSummary> CalculateBillingUnits(
            VsoPlanInfo plan,
            IEnumerable<BillingEvent> events,
            BillingSummary lastSummary,
            DateTime billSummaryEndTime,
            AzureLocation region,
            Dictionary<string, double> shardUsageTimes,
            IEnumerable<BillingEvent> allEnvironmentChangesForPlan,
            IDiagnosticsLogger logger)
        {
            var totalBillable = 0.0d;
            var environmentUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();
            var meterId = GetMeterIdByAzureLocation(plan.Location, logger.NewChildLogger());

            // Get events per environment
            var billingEventsPerEnvironment = events.GroupBy(b => b.Environment.Id);
            foreach (var environmentEvents in billingEventsPerEnvironment)
            {
                var billableUnits = 0.0d;
                var usageByState = new Dictionary<string, double>();
                var seqEvents = environmentEvents.OrderBy(t => t.Time);
                var environmentDetails = environmentEvents.First().Environment;
                var slices = new List<BillingWindowSlice>();
                var childLogger = logger.NewChildLogger();

                childLogger.AddBaseValue("CloudEnvironmentId", environmentEvents.Key);

                // Create an initial BillingWindowSlice to use as a starting point for billing calculations
                // The previous BillingSummary is used as the StartTime and EndTime. The environments
                // previous state comes from the list of environment details in the previous BillingSummary.
                BillingWindowSlice.NextState initialState;
                if (lastSummary?.UsageDetail?.Environments != null && lastSummary.UsageDetail.Environments.ContainsKey(environmentEvents.Key))
                {
                    var endPreviousEnvironment = lastSummary.UsageDetail.Environments[environmentEvents.Key];

                    initialState = new BillingWindowSlice.NextState
                    {
                        EnvironmentState = ParseEnvironmentState(endPreviousEnvironment.EndState),
                        Sku = endPreviousEnvironment.Sku,
                        TransitionTime = lastSummary.PeriodEnd,
                    };
                }
                else
                {
                    initialState = new BillingWindowSlice.NextState
                    {
                        EnvironmentState = CloudEnvironmentState.None,
                        Sku = environmentDetails.Sku,
                        TransitionTime = lastSummary.PeriodEnd,
                    };
                }

                IEnumerable<BillingWindowSlice> allSlices;
                var currState = initialState;

                // Loop through each billing event for the current environment.
                foreach (var evnt in seqEvents)
                {
                    (allSlices, currState) = await GenerateWindowSlices(billSummaryEndTime, currState, evnt, plan, childLogger);
                    if (allSlices.Any())
                    {
                        slices.AddRange(allSlices);
                    }
                }

                // Get the remainder or the entire window if there were no events.
                (allSlices, currState) = await GenerateWindowSlices(billSummaryEndTime, currState, null, plan, childLogger);
                if (allSlices.Any())
                {
                    slices.AddRange(allSlices);
                }

                // We might not have any billable slices. If we don't, let's not do anything
                if (slices.Any())
                {
                    var usageBySkuByState = new Dictionary<string, UsageDictionary>();
                    var (isError, errorState) = CheckForErrors(plan, billSummaryEndTime, environmentDetails.Id, currState.EnvironmentState, allEnvironmentChangesForPlan, childLogger);

                    CloudEnvironmentState finalEnvironmentStateOrError;
                    if (!isError)
                    {
                        finalEnvironmentStateOrError = currState.EnvironmentState;

                        // Aggregate the billable units for each slice created above.
                        foreach (var slice in slices)
                        {
                            billableUnits += CalculateVsoUnitsByTimeAndSku(slice, usageBySkuByState, childLogger);
                        }
                    }
                    else
                    {
                        finalEnvironmentStateOrError = errorState;

                        childLogger.LogError("billingworker_plan_correction");
                    }

                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = environmentDetails.Name,
                        EndState = finalEnvironmentStateOrError.ToString(),
                        Usage = new UsageDictionary
                        {
                            { meterId, billableUnits },
                        },

                        // Sku is currently not exposed in the billing report and only used in this method for tracking the previous end state
                        // so even if there are multiple Sku changes within this window we only need to remember the final state.
                        Sku = currState.Sku,
                    };

                    usageDetail.ResourceUsage = ReportResourceUsageDetails(usageBySkuByState, skuCatalog);

                    environmentUsageDetails.Add(environmentEvents.Key, usageDetail);
                    totalBillable += billableUnits;
                    RollupUsage(usageByState, usageBySkuByState);
                }

                LogEnvironmentUsageDetails(billSummaryEndTime, region, environmentDetails.Sku.Name, usageByState, childLogger);
                CopyEnvironmentStateTimesToShardStateTimes(usageByState, shardUsageTimes);
            }

            return new BillingSummary
            {
                PeriodStart = lastSummary.PeriodEnd,
                PeriodEnd = billSummaryEndTime,
                UsageDetail = new UsageDetail { Environments = environmentUsageDetails, },
                SubscriptionState = string.Empty,
                Plan = string.Empty,
                SubmissionState = BillingSubmissionState.None,
                Usage = new UsageDictionary
                {
                    { meterId, totalBillable },
                },
            };
        }

        /// <summary>
        /// This method will generate a BillingSummary that includes environments that did not have any billing events
        /// during the given period. The previous BillingSummary and the current BillingSummary are compared to determine
        /// if any environments exist in the previous but not in the current. For those environments a billing unit is
        /// calculated and appended to the current BillingSummary.
        /// </summary>
        /// <param name="plan">The plan the bill is being generated for.</param>
        /// <param name="currentSummary">The existing summary that we're appending to.</param>
        /// <param name="latestEvent">The last summary event. Note that this should have an args of type <see cref="BillingSummary"/>.</param>
        /// <param name="end">the end time for the bill.</param>
        /// <param name="region">The region the bill applies to.</param>
        /// <param name="shardUsageTimes">The aggregate usage time dictionary. Used to track how much time is being billed by this bill generation.</param>
        /// <param name="allEventsForPlan">All environment state changes for a given plan.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>An updated billing Summary.</returns>
        public async Task<BillingSummary> CaculateBillingForEnvironmentsWithNoEvents(
            VsoPlanInfo plan,
            BillingSummary currentSummary,
            BillingEvent latestEvent,
            DateTime end,
            AzureLocation region,
            Dictionary<string, double> shardUsageTimes,
            IEnumerable<BillingEvent> allEventsForPlan,
            IDiagnosticsLogger logger)
        {
            // Scenario: Environment has no billing Events in this billing period
            var lastSummary = (BillingSummary)latestEvent.Args;
            if (lastSummary == null || lastSummary.UsageDetail == null)
            {
                // TODO: design for no billing summary in this time period
                // We may want to widen our summary search
                // No previous summary environments
                logger.LogWarning("billing_no_prior_bill");
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

            var meterId = GetMeterIdByAzureLocation(plan.Location, logger.NewChildLogger());
            var envUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();

            // Loop through the missing environments and calculate billing.
            var missingEnvironments = lastPeriodEnvmts.Where(i => missingEnvmtIds.Contains(i.Key)).ToList();
            foreach (var environment in missingEnvironments)
            {
                var childLogger = logger.NewChildLogger();

                childLogger.AddBaseValue("CloudEnvironmentId", environment.Key);

                var envUsageDetail = environment.Value;

                if (envUsageDetail.EndState == nameof(CloudEnvironmentState.Deleted) ||
                    envUsageDetail.EndState == nameof(CloudEnvironmentState.Moved))
                {
                    // Environment state is Deleted or Moved so nothing new to bill.
                    continue;
                }

                var endState = new BillingWindowSlice.NextState
                {
                    EnvironmentState = ParseEnvironmentState(envUsageDetail.EndState),
                    Sku = envUsageDetail.Sku,
                    TransitionTime = lastSummary.PeriodEnd,
                };

                // Get the remainder or the entire window if there were no events.
                var (allSlices, nextState) = await GenerateWindowSlices(end, endState, null, plan, childLogger);

                var billableUnits = 0d;
                var usageBySkuByState = new Dictionary<string, UsageDictionary>();

                var (isError, errorState) = CheckForErrors(plan, end, environment.Key, nextState.EnvironmentState, allEventsForPlan, childLogger);

                CloudEnvironmentState finalEnvironmentStateOrError;
                if (!isError)
                {
                    finalEnvironmentStateOrError = nextState.EnvironmentState;

                    // Aggregate the billable units for each slice created above.
                    foreach (var slice in allSlices)
                    {
                        billableUnits += CalculateVsoUnitsByTimeAndSku(slice, usageBySkuByState, childLogger.NewChildLogger());
                    }
                }
                else
                {
                    finalEnvironmentStateOrError = errorState;

                    childLogger.LogError("billingworker_plan_correction");
                }

                var usageDetail = new EnvironmentUsageDetail
                {
                    Name = envUsageDetail.Name,
                    EndState = finalEnvironmentStateOrError.ToString(),
                    Usage = new UsageDictionary
                    {
                        { meterId, billableUnits },
                    },

                    // Sku is currently not exposed in the billing report and only used in this method for tracking the previous end state
                    // so even if there are multiple Sku changes within this window we only need to remember the final state.
                    Sku = environment.Value.Sku,
                };

                usageDetail.ResourceUsage = ReportResourceUsageDetails(usageBySkuByState, skuCatalog);

                // rollup usage by sku into their respective states
                var usageByState = usageBySkuByState
                    .Select(o => new { state = o.Key, usage = o.Value.Sum(x => x.Value) })
                    .ToDictionary(o => o.state, o => o.usage);

                // Update Environment list, SkuPlan billable units, and User billable units
                currentSummary.UsageDetail.Environments.Add(environment.Key, usageDetail);

                // Telemeter the environment bill data
                LogEnvironmentUsageDetails(end, region, envUsageDetail.Sku.Name, usageByState, childLogger);
                CopyEnvironmentStateTimesToShardStateTimes(usageByState, shardUsageTimes);

                if (currentSummary.Usage.ContainsKey(meterId))
                {
                    currentSummary.Usage[meterId] = currentSummary.Usage[meterId] + billableUnits;
                }
                else
                {
                    currentSummary.Usage.Add(meterId, billableUnits);
                }
            }

            return currentSummary;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteInner(IDiagnosticsLogger logger, DateTime start, DateTime end, string planShard, AzureLocation region)
        {
            await logger.OperationScopeAsync(
               $"{ServiceName}_begin_shard_calculations",
               async (childLogger) =>
               {
                   // Get the list of billable plans - this includes all active plans + deleted plans that have not yet had a final bill submitted.
                   var plans = await planManager.GetBillablePlansByShardAsync(
                       new List<AzureLocation> { region },
                       planShard,
                       childLogger);

                   var shardUsageStateTimes = new Dictionary<string, double>();
                   foreach (var plan in plans)
                   {
                       await BeginAccountCalculations(plan, start, end, childLogger.NewChildLogger(), region, shardUsageStateTimes);
                   }

                   if (shardUsageStateTimes.ContainsKey(BillingWindowBillingState.Active.ToString()))
                   {
                       childLogger.AddValue("BillableActiveSeconds", shardUsageStateTimes[BillingWindowBillingState.Active.ToString()].ToString());
                   }

                   if (shardUsageStateTimes.ContainsKey(BillingWindowBillingState.Inactive.ToString()))
                   {
                       childLogger.AddValue("BillableInactiveSeconds", shardUsageStateTimes[BillingWindowBillingState.Inactive.ToString()].ToString());
                   }

                   childLogger.FluentAddValue("Shard", planShard)
                              .FluentAddValue("Location", region.ToString())
                              .FluentAddValue("BillEndingTime", end.ToString())
                              .LogInfo("billing_aggregate_shard_summary");
               });
        }

        private static ResourceUsageDetail ReportResourceUsageDetails(
            Dictionary<string, UsageDictionary> usageBySkuByState,
            ISkuCatalog skuCatalog)
        {
            var active = nameof(BillingWindowBillingState.Active);

            var activeResources = new[] { Compute, Storage };
            var inactiveResources = new[] { Storage };

            var skuUsageByResource =
               (from usageBySkuForAState in usageBySkuByState // depivot by state
                let state = usageBySkuForAState.Key
                let usageBySku = usageBySkuForAState.Value
                from usageForASku in usageBySku // depivot by sku
                let sku = usageForASku.Key
                let usage = usageForASku.Value
                from resource in state == active ? activeResources : inactiveResources // unroll resource
                let resourceSkuStateUsage = new
                {
                    State = state,
                    Resource = resource,
                    Sku = sku,
                    Usage = usage,
                }
                group resourceSkuStateUsage by new
                {
                    resourceSkuStateUsage.Resource,
                    resourceSkuStateUsage.Sku,
                }
                into usageBySkuResource
                let sku = usageBySkuResource.Key.Sku
                let size = skuCatalog.CloudEnvironmentSkus[sku].StorageSizeInGB // denormalize storage size
                select new
                {
                    usageBySkuResource.Key.Resource,
                    Sku = sku,
                    Size = size,
                    Usage = usageBySkuResource.Sum(x => x.Usage), // rollup (storage) by active/inactive
                }).ToLookup(o => o.Resource); // pivot by resource

            var storageList = skuUsageByResource[Storage].Select(skuUsage =>
                new StorageUsageDetail()
                {
                    Sku = skuUsage.Sku,
                    Usage = skuUsage.Usage,
                    Size = skuUsage.Size,
                }).ToList();

            var computeList = skuUsageByResource[Compute].Select(skuUsage =>
                new ComputeUsageDetail()
                {
                    Sku = skuUsage.Sku,
                    Usage = skuUsage.Usage,
                }).ToList();

            if (computeList.Count == 0)
            {
                computeList = null;
            }

            return new ResourceUsageDetail()
            {
                Storage = storageList,
                Compute = computeList,
            };
        }

        private static void RollupUsage(UsageDictionary usageByState, Dictionary<string, UsageDictionary> usageBySkuByState)
        {
            // rollup usage for for sku's into their respective states
            var usageByStateByAEnv =
               (from usageBySkuByAState in usageBySkuByState // dict of dict => key/value pairs where {key = active|inactive, value = dict}
                group usageBySkuByAState by usageBySkuByAState.Key into usageBySkuByAState // group pairs by acitve|inactive
                let state = usageBySkuByAState.Key // active|inactive
                let usageByASku = usageBySkuByAState.SelectMany(o => o.Value) // select the usageBySku dict of each pair and flatten to usage/sku key/value pairs
                let usage = usageByASku.Select(o => o.Value) // ignore the key (sku) and select just the value (seconds)
                select new { state, usage = usage.Sum() }) // sum the seconds per state
                .ToDictionary(o => o.state, o => o.usage);

            foreach (var pair in usageByStateByAEnv)
            {
                var key = pair.Key;
                var value = pair.Value;
                usageByState[key] = (usageByState.ContainsKey(key) ? usageByState[key] : 0) + value;
            }
        }

        private static void CopyEnvironmentStateTimesToShardStateTimes(UsageDictionary environmentStateTimes, UsageDictionary shardUsageTimes)
        {
            foreach (var stateTime in environmentStateTimes)
            {
                // Add times to the global sharded usage times list for querying
                if (shardUsageTimes.ContainsKey(stateTime.Key))
                {
                    shardUsageTimes[stateTime.Key] += stateTime.Value;
                }
                else
                {
                    shardUsageTimes[stateTime.Key] = stateTime.Value;
                }
            }
        }

        /// <summary>
        /// Helper method to parse a cloud environment state from its string representation.
        /// </summary>
        /// <param name="state">The string representing the state being parsed.</param>
        /// <returns>The parsed state enum value.</returns>
        private static CloudEnvironmentState ParseEnvironmentState(string state)
        {
            return (CloudEnvironmentState)Enum.Parse(typeof(CloudEnvironmentState), state);
        }

        /// <summary>
        /// Helper method to extract state transition settings from a given BillingEvent.
        /// </summary>
        /// <param name="billingEvent">The billing event.</param>
        /// <returns>A NextState which contains state transition metadata from the event.</returns>
        private static BillingWindowSlice.NextState BuildNextStateFromEvent(BillingEvent billingEvent)
        {
            return new BillingWindowSlice.NextState
            {
                EnvironmentState = ParseEnvironmentState(((BillingStateChange)billingEvent.Args).NewValue),
                Sku = billingEvent.Environment.Sku,
                TransitionTime = billingEvent.Time,
            };
        }

        private void LogEnvironmentUsageDetails(DateTime end, AzureLocation region, string sku, UsageDictionary usageStateTimes, IDiagnosticsLogger logger)
        {
            if (usageStateTimes.ContainsKey(BillingWindowBillingState.Active.ToString()))
            {
                logger.AddValue("BillableActiveSeconds", usageStateTimes[BillingWindowBillingState.Active.ToString()].ToString());
            }

            if (usageStateTimes.ContainsKey(BillingWindowBillingState.Inactive.ToString()))
            {
                logger.AddValue("BillableInactiveSeconds", usageStateTimes[BillingWindowBillingState.Inactive.ToString()].ToString());
            }

            logger.FluentAddValue("EnvironmentSku", sku.ToString())
                  .FluentAddValue("Location", region.ToString())
                  .FluentAddValue("BillEndingTime", end.ToString())
                  .LogInfo("billing_aggregate_environment_summary");
        }

        private void CheckForMissingEnvironments(BillingSummary billingSummary, DateTime end, IEnumerable<BillingEvent> allEnvironmentEvents, IDiagnosticsLogger childLogger)
        {
            // Get all the events that happened before the current billing cycle.
            var allOlderEnvironmentEvents = allEnvironmentEvents.Where(x => x.Time < end);

            // Group all events by their env id
            var envsGroupedByEnvironments = allOlderEnvironmentEvents.GroupBy(x => x.Environment.Id);
            foreach (var env in envsGroupedByEnvironments)
            {
                // Do we already have some sort of record of this environmnet. If so, do nothing.
                if (billingSummary?.UsageDetail?.Environments != null && billingSummary.UsageDetail.Environments.ContainsKey(env.Key))
                {
                    continue;
                }

                var lastEnvEventChange = GetLastBillableEventStateChange(env);
                if (lastEnvEventChange is null)
                {
                    // If we get this far, we could not find an appropriate billing event. Perhaps it never became available after this time range started. If so, do nothing.
                    continue;
                }

                // We have the latest billable state. Let's make sure it's not already a deleted environment.
                var lastState = ((BillingStateChange)lastEnvEventChange.Args).NewValue;
                if (lastState == nameof(CloudEnvironmentState.Deleted))
                {
                    continue;
                }

                // Generate a fake entry for this billing summary so we do not lose sight of the environment.
                var envUsageDetails = new EnvironmentUsageDetail
                {
                    EndState = lastState,
                    Sku = lastEnvEventChange.Environment.Sku,
                    Usage = new UsageDictionary(),
                    Name = lastEnvEventChange.Environment.Name,
                };

                // Add the lost environnment to the summary for future tracking.
                billingSummary.UsageDetail.Environments.Add(env.Key, envUsageDetails);
            }
        }

        private (bool isError, CloudEnvironmentState errorState) CheckForErrors(VsoPlanInfo plan, DateTime end, string id, CloudEnvironmentState expectedState, IEnumerable<BillingEvent> allEventsForPlan, IDiagnosticsLogger logger)
        {
            // We're deleted already, we can trust that's a terminal state
            if (expectedState == CloudEnvironmentState.Deleted)
            {
                return (false, expectedState);
            }

            var envEventsSinceStart = allEventsForPlan.Where(x => x.Environment.Id == id);

            if (envEventsSinceStart.Any())
            {
                var lastState = GetLastBillableEventStateChange(envEventsSinceStart);
                if (lastState is null)
                {
                    // If we get this far, we could not find an appropriate billing event
                    logger.LogError("billingworker_missing_events_for_environments");
                    return (true, CloudEnvironmentState.Deleted);
                }

                var lastBillingStateChange = ParseEnvironmentState((lastState.Args as BillingStateChange).NewValue);
                return (lastBillingStateChange != expectedState, lastBillingStateChange);
            }

            // If we get this far, our we did not get any billing events. We should no longer be billing the customer and should investigate
            logger.LogError("billingworker_missing_events_for_environments");
            return (true, CloudEnvironmentState.Deleted);
        }

        private async Task<IEnumerable<BillingEvent>> GetAllEnvironmentStateChanges(VsoPlanInfo plan, DateTime end, IDiagnosticsLogger logger)
        {
            // We need a list of all environment state changes in this billing Plan.
            Expression<Func<BillingEvent, bool>> filter = bev => bev.Plan.Subscription == plan.Subscription &&
                                                   bev.Plan.ResourceGroup == plan.ResourceGroup &&
                                                   bev.Plan.Name == plan.Name &&
                                                   bev.Time < end &&
                                                   bev.Type == BillingEventTypes.EnvironmentStateChange;

            var envEventsSinceStart = await billingEventManager.GetPlanEventsAsync(filter, logger.NewChildLogger());
            var planNamespace = plan.ProviderNamespace ?? VsoPlanInfo.VsoProviderNamespace;
            var eventsFileteredByNamespace = envEventsSinceStart.Where(x => (x.Plan.ProviderNamespace ?? VsoPlanInfo.VsoProviderNamespace) == planNamespace);
            return eventsFileteredByNamespace;
        }

        private BillingEvent GetLastBillableEventStateChange(IEnumerable<BillingEvent> envEventsSinceStart)
        {
            BillingEvent lastState = null;
            foreach (var billEvent in envEventsSinceStart.OrderBy(x => x.Time))
            {
                var billingStateChange = (BillingStateChange)billEvent.Args;
                var newValue = billingStateChange.NewValue;

                if (newValue.Equals(nameof(CloudEnvironmentState.Deleted)))
                {
                    return billEvent; // The environment was deleted. We are done and shouldn't consider other states. Deleted is the terminal state.
                }

                if (newValue.Equals(nameof(CloudEnvironmentState.Available)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Shutdown)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Moved)) ||
                    newValue.Equals(nameof(CloudEnvironmentState.Deleted)))
                {
                    lastState = billEvent;
                }
            }

            return lastState;
        }

        private async Task<(IEnumerable<BillingWindowSlice> Slices, BillingWindowSlice.NextState NextState)> GenerateWindowSlices(DateTime end, BillingWindowSlice.NextState currState, BillingEvent evnt, VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            var (currSlice, nextState) = CalculateNextWindow(evnt, currState, end);

            if (currSlice is null)
            {
                // there are no state machine transitions here so just bail out
                return (Enumerable.Empty<BillingWindowSlice>(), currState);
            }

            var slicesWithOverrides = await GenerateSlicesWithOverrides(currSlice, plan, logger);
            return (slicesWithOverrides, nextState);
        }

        private async Task<IEnumerable<BillingWindowSlice>> GenerateSlicesWithOverrides(BillingWindowSlice currSlice, VsoPlanInfo plan, IDiagnosticsLogger logger)
        {
            var dividedSlices = GenerateHourBoundTimeSlices(currSlice);
            foreach (var slice in dividedSlices)
            {
                var currBillingOverride = await billingEventManager.GetOverrideStateForTimeAsync(slice.StartTime, plan.Subscription, plan, slice.Sku, logger.NewChildLogger());
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
                    if (currSlice.EndTime > endTime)
                    {
                        // We need to loop again and add a new slice for this hour segment since this segment is a subset.
                        lastSlice = new BillingWindowSlice()
                        {
                            StartTime = lastSlice.EndTime,
                            EndTime = endTime,
                            BillingState = currSlice.BillingState,
                            Sku = currSlice.Sku,
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
                            Sku = currSlice.Sku,
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
        /// <param name="currentEvent">The current event being processed.</param>
        /// <param name="currentState">the current state (sku, time) for the state machine.</param>
        /// <param name="endTimeForPeriod">the overall end time for the period.</param>
        /// <returns>A tuple representing the next slice/state.</returns>
        private (BillingWindowSlice Slice, BillingWindowSlice.NextState NextState) CalculateNextWindow(BillingEvent currentEvent, BillingWindowSlice.NextState currentState, DateTime endTimeForPeriod)
        {
            // If we're looking at a null current Event and the last event status is not Deleted,
            // just calculate the time delta otherwise, run through the state machine again.
            if (currentEvent == null)
            {
                // No new time slice to create if last status was Deleted.
                // There will be no more events after a Deleted event.
                if (currentState.EnvironmentState == CloudEnvironmentState.Available || currentState.EnvironmentState == CloudEnvironmentState.Shutdown)
                {
                    var finalSlice = new BillingWindowSlice()
                    {
                        StartTime = currentState.TransitionTime,
                        EndTime = endTimeForPeriod,

                        BillingState = currentState.EnvironmentState == CloudEnvironmentState.Shutdown ?
                            BillingWindowBillingState.Inactive : BillingWindowBillingState.Active,
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
                        // CloudEnvironmentState has gone from Shutdown to Available or Deleted
                        // in this BillingWindowSlice.
                        case CloudEnvironmentState.Available:
                        case CloudEnvironmentState.Moved:
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

                // No previous billing summary
                case CloudEnvironmentState.None:
                    switch (nextState.EnvironmentState)
                    {
                        case CloudEnvironmentState.Available:
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
        /// Helper method to perform the billing unit calculation. This method accepts the BillingWindowSlice and sku
        /// name, returning the billable unit for the given period of time.
        /// </summary>
        /// <param name="slice">the last active billing slice.</param>
        /// <param name="usageBySkuByState">the per environment sku telemetered usage times.</param>
        /// <returns>the total billable time.</returns>
        private double CalculateVsoUnitsByTimeAndSku(BillingWindowSlice slice, Dictionary<string, UsageDictionary> usageBySkuByState, IDiagnosticsLogger logger)
        {
            // check if billing is disabled for this time slice
            if (slice.OverrideState != BillingOverrideState.BillingEnabled)
            {
                return 0;
            }

            var unitsPerHour = GetVsoUnitsForSlice(slice, logger);
            const decimal secondsPerHour = 3600m;
            var unitsPerSecond = unitsPerHour / secondsPerHour;
            var seconds = (decimal)slice.EndTime.Subtract(slice.StartTime).TotalSeconds;
            var units = seconds * unitsPerSecond;
            var state = slice.BillingState.ToString();
            var sku = slice.Sku.Name;

            if (!usageBySkuByState.ContainsKey(state))
            {
                usageBySkuByState[state] = new UsageDictionary();
            }

            var usageBySku = usageBySkuByState[state];
            if (usageBySku.ContainsKey(sku))
            {
                usageBySku[sku] += (double)seconds;
            }
            else
            {
                usageBySku[sku] = (double)seconds;
            }

            return (double)units;
        }

        /// <summary>
        /// Helper method to return active or inactive VSO units from CloudEnvironmentSku<see cref="CloudEnvironmentSku"/> name.
        /// </summary>
        /// <param name="slice">The billing slice being evaluated.</param>
        /// <returns>The units for a given slice.</returns>
        private decimal GetVsoUnitsForSlice(BillingWindowSlice slice, IDiagnosticsLogger logger)
        {
            var billingState = slice.BillingState;

            if (slice.Sku == null)
            {
                logger.LogError("billingworker_getVSOUnits_sku_error");
                return 0.0m;
            }

            var skuName = slice.Sku.Name;

            if (skuDictionary.TryGetValue(skuName, out var sku))
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
                    logger.AddValue("BillingWindowState", billingState.ToString());
                    logger.LogError("BillingWorker_GetVSOUnit_UnsupportedState");
                    return 0;
                }
            }
            else
            {
                logger.AddValue("SkuName", skuName);
                logger.LogError("BillingWorker_GetVSOUnit_UnsupportedSku");
                return 0.0m;
            }
        }

        /// <summary>
        /// Helper method to find billing meter Id by AzureLocation.
        /// </summary>
        /// <param name="location">The location to get the meterID for.</param>
        /// <returns>The string representing the meterID.</returns>
        private string GetMeterIdByAzureLocation(AzureLocation location, IDiagnosticsLogger logger)
        {
            if (meterDictionary.TryGetValue(location, out var id))
            {
                return id;
            }
            else
            {
                logger.FluentAddValue("Azurelocation", location.ToString());
                logger.LogError("BillingWorker_GetMeterID_UnsupportedLocation");
                return "METER_NOT_FOUND";
            }
        }
    }
}
