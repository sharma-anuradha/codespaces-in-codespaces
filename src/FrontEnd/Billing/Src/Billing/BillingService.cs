// <copyright file="BillingService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using UsageDictionary = System.Collections.Generic.Dictionary<string, double>;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Calculates billing units per VSO Account in the current control plane region(s)
    /// and saves a BillingSummary to the environment_billing_events table.
    /// </summary>
    public partial class BillingService : IBillingService
    {
        private readonly IBillingEventManager billingEventManager;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;
        private readonly IDiagnosticsLogger logger;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly string billingEventType = BillingEventTypes.BillingSummary;
        private readonly string DeletedEnvState = nameof(CloudEnvironmentState.Deleted);
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> SkuDictionary;
        private readonly string billingWorkerLogBase = "billing-worker";
        // TODO: move the meter Dictionary to AppSettings
        private readonly IDictionary<AzureLocation, string> meterDictionary = new Dictionary<AzureLocation, string>
        {
            {AzureLocation.EastUs, "91f658f0-9ce6-4f5d-8795-aa224cb83ccc" },
            {AzureLocation.WestUs2, "5f3afa79-01ad-4d7e-b691-73feca4ea350" },
            {AzureLocation.WestEurope, "edd2e9c5-56ce-469f-aedb-ba82f4e745cd" },
            {AzureLocation.SouthEastAsia, "12e4ab51-ee20-4bbc-95a6-9dddea31e634" },
        };

        /// <summary>
        /// According to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
        /// https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/WopiFrame.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Onboarding.one%7C55f62e8d-ea91-4a90-982c-04899e106633%2FFAQ%7C25cc6e79-1e39-424d-9403-cd05d2f675e9%2F%29&wdorigin=703
        /// </summary>
        private readonly int lookBackThresholdHrs = 48;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingService"/> class.
        /// </summary>
        /// <param name="billingEventManager"></param>
        /// <param name="controlPlaneInfo"></param>
        public BillingService(IBillingEventManager billingEventManager,
                            IControlPlaneInfo controlPlaneInfo,
                            ISkuCatalog skuCatalog,
                            IDiagnosticsLogger diagnosticsLogger,
                            IClaimedDistributedLease claimedDistributedLease)
        {
            Requires.NotNull(billingEventManager, nameof(billingEventManager));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(skuCatalog, nameof(skuCatalog));
            Requires.NotNull(diagnosticsLogger, nameof(diagnosticsLogger));
            Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));

            this.billingEventManager = billingEventManager;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
            SkuDictionary = this.skuCatalog.CloudEnvironmentSkus;
            logger = diagnosticsLogger;
            this.claimedDistributedLease = claimedDistributedLease;

            // Send all logs to billingservices Geneva logger table
            logger.FluentAddBaseValue("Service", "billingservices");
        }

        /// <summary>
        /// Generates BillingSummary<see cref="BillingSummary"/> records per accounts in the
        /// current control plane region(s). The method creates subscriptionId shards in the form
        /// "a-westus2", "1-westus2" for every char of a 16 bit GUID.
        /// Background tasks are created per shard to process subscriptions.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task GenerateBillingSummary(IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var concurrentRuns = 0;
            var start = DateTime.UtcNow.Subtract(TimeSpan.FromHours(lookBackThresholdHrs));
            // TODO: consider on the hour billing summary creation. For example summarys would have a time range of
            // 12:00:00 -> 1:00:00
            var end = DateTime.UtcNow;
            var controlPlaneRegions = controlPlaneInfo.GetAllDataPlaneLocations().Shuffle();
            var accountShards = billingEventManager.GetShards();
            var accountsToRegionsShards = accountShards.SelectMany(x => controlPlaneRegions, (accoundShard, region) => new { accoundShard, region });
            var taskHelper = new TaskHelper(logger);
            logger.FluentAddBaseValue("startCalculationTime", start);
            logger.FluentAddBaseValue("endCalculationTime", end);

            await taskHelper.RunBackgroundEnumerableAsync(
                $"{billingWorkerLogBase}-run",
                accountsToRegionsShards,
                async (billingAccountShard, childlogger) =>
                 {
                     var leaseName = $"billingworkerrun-{billingAccountShard.accoundShard}-{billingAccountShard.region}";
                     childlogger.FluentAddBaseValue("Service", "billingservices");
                     childlogger.FluentAddBaseValue("leaseName", leaseName);
                     using (var lease = await claimedDistributedLease.Obtain($"{billingWorkerLogBase}-leases", leaseName, TimeSpan.FromHours(1),
                         childlogger))
                     {
                         if (lease != null)
                         {
                             concurrentRuns++;
                             var accounts = await billingEventManager.GetAccountsByShardAsync(
                                                                     start,
                                                                     end,
                                                                     logger,
                                                                     new List<AzureLocation> { billingAccountShard.region },
                                                                     billingAccountShard.accoundShard);
                             foreach (var account in accounts)
                             {
                                 await BeginAccountCalculations(account, start, end, childlogger);
                             }

                             concurrentRuns--;
                         }
                     }
                 },
                logger);
        }

        /// <summary>
        /// This method controls the main logic flow of generating BillingSummary records.
        /// The last BillingSummary is fetched and used as the seed event for the billing unit
        /// calculations on the current events.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async Task BeginAccountCalculations(VsoAccountInfo account, DateTime start, DateTime end, IDiagnosticsLogger logger)
        {
            logger.AddAccount(account)
                .FluentAddBaseValue("startCalculationTime", start)
                .FluentAddBaseValue("endCalculationTime", end);

            await logger.OperationScopeAsync(
                $"{billingWorkerLogBase}-begin-account-calculations",
                async (childLogger) =>
                {
                    // Get the last BillingSummary.
                    var summaryEvents = await billingEventManager.GetAccountEventsAsync(account, start, end, new string[] { billingEventType }, logger);
                    BillingEvent latestSummary = new BillingEvent();
                    if (!summaryEvents.Any())
                    {
                        // No summary events exist for this account eg. first run of the BillingWorker
                        // Create a generic BillingSummary to use as a starting point for the following
                        // billing calculations, and seed it with this billing period's start time.
                        latestSummary.Time = start;
                        latestSummary.Args = new BillingSummary
                        {
                            PeriodEnd = start,
                        };
                    }
                    else
                    {
                        latestSummary = summaryEvents.OrderByDescending(b => b.Time).First();

                        // If the lastSummary is less than 5 minutes old, we do not want to generate another summary
                        // This helps give some buffer to the background tasks picking up work.
                        if (DateTime.UtcNow.Subtract(latestSummary.Time).TotalMinutes < 5)
                        {
                            return;
                        }
                    }

                    // Get EnvironmentStateChnage events for the given account during the given period.
                    var billingEvents = await billingEventManager.GetAccountEventsAsync(account, latestSummary.Time, end, new string[] { BillingEventTypes.EnvironmentStateChange }, logger);

                    // Using the above EnvironmentStateChange events and the previous BillingSummary create the current BillingSummary.
                    var billingSummary = CalculateBillingUnits(account, billingEvents, (BillingSummary)latestSummary.Args, end);

                    // Append to the current BillingSummary any environments that did not have billing events during this period, but were present in the previous BillingSummary.
                    var totalBillingSummary = CaculateBillingForEnvironmentsWithNoEvents(account, billingSummary, latestSummary, end);
                    await billingEventManager.CreateEventAsync(account, null, billingEventType, totalBillingSummary, logger);
                },
                swallowException: true);

            // TODO: Need to handle subscriptionState events (No billing for suspended subscriptions)
        }

        /// <summary>
        /// This method loops through all the BillingEvents<see cref="BillingEvent"/> in this current billing period and generates a
        /// BillingSummary. The last BillingSummary is used as the starting point for billing unit calculations.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="events"></param>
        /// <param name="startSummary"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public BillingSummary CalculateBillingUnits(
            VsoAccountInfo account,
            IEnumerable<BillingEvent> events,
            BillingSummary startSummary,
            DateTime end)
        {
            var totalBillable = 0.0d;
            var environmentUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();
            var userUsageDetails = new Dictionary<string, UserUsageDetail>();
            var meterId = GetMeterIdByAzureLocation(account.Location);

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
                var initialSlice = new BillingWindowSlice()
                {
                    BillingState = BillingWindowBillingState.Active,
                    StartTime = startSummary.PeriodEnd,
                    EndTime = startSummary.PeriodEnd,
                    LastState = ParseEnvironmentState(startSummary.UsageDetail?.Environments[environmentEvents.Key].EndState ?? "None"),
                };

                var currSlice = initialSlice;

                // Loop through each billing event for the current environment.
                foreach (var evnt in seqEvents)
                {
                    currSlice = CalculateNextWindow(evnt, currSlice, end);
                    if (currSlice != null)
                    {
                        slices.Add(currSlice);
                    }
                }

                // Get the remainder or the entire window if there were no events.
                currSlice = CalculateNextWindow(null, currSlice, end);
                if (currSlice != null)
                {
                    slices.Add(currSlice);
                }

                // Aggregate the billable units for each slice created above.
                slices.ForEach((slice) => billable += CalculateVsoUnitsByTimeAndSku(slice, environmentDetails.Sku.Name));
                var usageDetail = new EnvironmentUsageDetail
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
                environmentUsageDetails.Add(environmentEvents.Key, usageDetail);
                userUsageDetails = AggregateUserUsageDetails(userUsageDetails, billable, environmentDetails.UserId, meterId);
                totalBillable += billable;
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
                if (lastSlice.LastState == CloudEnvironmentState.Deleted)
                {
                    return null;
                }

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
        /// <param name="account"></param>
        /// <param name="currentSummary"></param>
        /// <param name="latestEvent"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public BillingSummary CaculateBillingForEnvironmentsWithNoEvents(
            VsoAccountInfo account,
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
                logger.AddAccount(account);
                logger.LogWarning("No BillingSummary found in the last 48 hours for the account.");
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

            var meterId = GetMeterIdByAzureLocation(account.Location);
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
                    EndTime = latestEvent.Time,
                    LastState = endState,
                    StartTime = latestEvent.Time,
                };

                var currentWindowSlice = CalculateNextWindow(null, billingSlice, end);
                var billable = CalculateVsoUnitsByTimeAndSku(currentWindowSlice, envUsageDetail.Sku.Name);
                var usageDetail = new EnvironmentUsageDetail
                {
                    Name = envUsageDetail.Name,
                    EndState = currentWindowSlice.LastState.ToString(),
                    Usage = new UsageDictionary
                    {
                        { meterId, billable },
                    },
                    Sku = environment.Value.Sku,
                    UserId = environment.Value.UserId,
                };

                // Update Environment list, Account billable units, and User billable units
                currentSummary.UsageDetail.Environments.Add(environment.Key, usageDetail);
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
            var vsoUnitsPerHour = GetVsoUnitsBySkuName(sku, slice.BillingState);
            const decimal secondsPerHour = 3600m;
            var vsoUnitsPerSecond = vsoUnitsPerHour / secondsPerHour;
            var totalSeconds = (decimal)slice.EndTime.Subtract(slice.StartTime).TotalSeconds;
            var vsoUnits = totalSeconds * vsoUnitsPerSecond;
            return (double)vsoUnits;
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
                    logger.AddValue("BillingWindowState", billingState.ToString());
                    logger.LogError("BillingWindowState is in an unsupported state.");
                    return 0;
                }
            }
            else
            {
                logger.FluentAddBaseValue("SkuName", name);
                logger.LogError("A Sku is being used on an environment that does not have a corresponding Sku catelog record.");
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
                logger.FluentAddBaseValue("Azurelocation", location.ToString());
                logger.LogError("An Azure location was used that does not have a corresponding meter Id.");
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
