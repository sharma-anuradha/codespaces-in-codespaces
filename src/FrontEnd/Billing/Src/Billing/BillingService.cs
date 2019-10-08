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
    public class BillingService : IBillingService
    {
        private readonly IBillingEventManager billingEventManager;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;
        private readonly IDiagnosticsLogger logger;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly string billingEventType = BillingEventTypes.BillingSummary;
        private readonly string DeletedEnvState = nameof(CloudEnvironmentState.Deleted);
        private readonly string AvailableEnvState = nameof(CloudEnvironmentState.Available);
        private readonly IReadOnlyDictionary<string, ICloudEnvironmentSku> SkuDictionary;
        private readonly string billingWorkerLogBase = "billing-worker";

        /// <summary>
        /// Accoring to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
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
        }

        /// <summary>
        /// Generates billing events per accounts in the current control plane regions.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task GenerateBillingSummary(IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // list of potential single char GUID values
            var rand = new Random();
            var accountShards = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();
            var concurrentRuns = 0;
            var start = DateTime.UtcNow.Subtract(TimeSpan.FromHours(lookBackThresholdHrs));
            var end = DateTime.UtcNow;
            var controlledRegions = controlPlaneInfo.GetAllDataPlaneLocations().ToList().Shuffle();
            var accountsToRegions = accountShards.SelectMany(x => controlledRegions, (accoundShard, region) => new { accoundShard, region });

            var taskHelper = new TaskHelper(logger);
            logger.FluentAddBaseValue("startCalculationTime", start);
            logger.FluentAddBaseValue("endCalculationTime", end);

            await taskHelper.RunBackgroundEnumerableAsync(
                $"{billingWorkerLogBase}-run",
                accountsToRegions,
                async (x, childlogger) =>
                 {
                     var leaseName = $"billingworkerrun-{x.accoundShard}-{x.region}";
                     childlogger.FluentAddBaseValue("leaseName", leaseName);
                     using (var lease = await claimedDistributedLease.Obtain($"{billingWorkerLogBase}-leases", leaseName, TimeSpan.FromHours(1), childlogger))
                     {
                         if (lease != null)
                         {
                             concurrentRuns++;
                             var accounts = await billingEventManager.GetAccountsByShardAsync(
                                                                     start,
                                                                     end,
                                                                     logger,
                                                                     new List<AzureLocation> { x.region },
                                                                     x.accoundShard);
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

        private async Task BeginAccountCalculations(VsoAccountInfo account, DateTime start, DateTime end, IDiagnosticsLogger logger)
        {
            logger.AddAccount(account)
                .FluentAddBaseValue("startCalculationTime", start)
                .FluentAddBaseValue("endCalculationTime", end);

            await logger.OperationScopeAsync(
                $"{billingWorkerLogBase}-begin-account-calculations",
                async (childLogger) =>
                {
                    var summaryEvents = await billingEventManager.GetAccountEventsAsync(account, start, end, new string[] { billingEventType }, logger);
                    BillingEvent latestSummary = new BillingEvent();
                    if (!summaryEvents.Any())
                    {
                        // No summary events exist for this account eg. first run of the BillingWorker
                        latestSummary.Time = start;
                    }
                    else
                    {
                        latestSummary = summaryEvents.OrderByDescending(b => b.Time).First();
                    }

                    var billingEvents = await billingEventManager.GetAccountEventsAsync(account, latestSummary.Time, end, new string[] { BillingEventTypes.EnvironmentStateChange }, logger);
                    var billingSummary = CalculateBillingUnits(billingEvents, latestSummary.Time, end, logger);
                    var totalBillingSummary = CaculateBillingForMissingEnvironments(billingSummary, latestSummary, latestSummary.Time, end);
                    await billingEventManager.CreateEventAsync(account, null, billingEventType, totalBillingSummary, logger);
                },
                swallowException: true);

            // TODO: Need to handle subscriptionState events (No billing for suspended subscriptions)
        }

        private BillingSummary CalculateBillingUnits(IEnumerable<BillingEvent> events, DateTime start, DateTime end, IDiagnosticsLogger logger)
        {
            var totalBillable = 0.0d;
            var envUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();
            var userUsageDetails = new Dictionary<string, UserUsageDetail>();

            // Get events per environment
            var perEnvironment = events.GroupBy(b => b.Environment.Id);
            foreach (var environment in perEnvironment)
            {
                var billable = 0.0d;
                var seqEvents = environment.ToList();
                var availableEvent = seqEvents.Where(e =>
                ((BillingStateChange)e.Args).NewValue.Equals(AvailableEnvState, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                // Scenario: Environment was deleted during this period
                var deletedEvent = seqEvents.Where(e =>
                ((BillingStateChange)e.Args).NewValue.Equals(DeletedEnvState, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (deletedEvent != null)
                {
                    if (availableEvent != null)
                    {
                        // Environment was made available during this time period
                        billable = CalculateActiveVsoUnitsByTimeAndSku(availableEvent.Time, deletedEvent.Time, deletedEvent.Environment.Sku.Name);
                    }
                    else
                    {
                        // Environment was made available outside this time period
                        // so use this period's start time
                        billable = CalculateActiveVsoUnitsByTimeAndSku(start, deletedEvent.Time, deletedEvent.Environment.Sku.Name);
                    }

                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = deletedEvent.Environment.Name,
                        EndState = DeletedEnvState,
                        Usage = new UsageDictionary
                            {
                                { "METER", billable },
                            },
                        Sku = deletedEvent.Environment.Sku,
                        UserId = deletedEvent.Environment.UserId,
                    };
                    envUsageDetails.Add(environment.Key, usageDetail);
                    userUsageDetails = AggregateUserUsageDetails(userUsageDetails, billable, deletedEvent.Environment.UserId);
                    totalBillable += billable;
                    continue;

                    // No need to look at any other billing events for this time period.
                }

                // Scenario: Environment was made available during this period
                if (availableEvent != null)
                {
                    billable = CalculateActiveVsoUnitsByTimeAndSku(availableEvent.Time, end, availableEvent.Environment.Sku.Name);
                    totalBillable += billable;
                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = availableEvent.Environment.Name,
                        EndState = AvailableEnvState,
                        Usage = new UsageDictionary
                            {
                                { "METER", billable },
                            },
                        Sku = availableEvent.Environment.Sku,
                        UserId = availableEvent.Environment.UserId,
                    };
                    envUsageDetails.Add(environment.Key, usageDetail);
                    userUsageDetails = AggregateUserUsageDetails(userUsageDetails, billable, availableEvent.Environment.UserId);
                }
            }

            return new BillingSummary
            {
                PeriodStart = start,
                PeriodEnd = end,
                UsageDetail = new UsageDetail { Environments = envUsageDetails, Users = userUsageDetails, },
                SubscriptionState = string.Empty,
                Plan = string.Empty,
                Emitted = false,
                Usage = new UsageDictionary
                {
                    { "METER", totalBillable },
                },
            };
        }

        private BillingSummary CaculateBillingForMissingEnvironments(BillingSummary currentSummary, BillingEvent latestSummary, DateTime start, DateTime end)
        {
            // Scenario: Environment has no billing Events in this billing period
            var currentPeriodEnvmts = currentSummary.UsageDetail.Environments;
            if (((BillingSummary)latestSummary.Args) == null)
            {
                // No previous summary environments
                return currentSummary;
            }
            var lastPeriodEnvmts = ((BillingSummary)latestSummary.Args).UsageDetail.Environments;
            var missingEnvmtIds = lastPeriodEnvmts.Keys.Where(i => !currentPeriodEnvmts.ContainsKey(i)).ToList();
            if (!missingEnvmtIds.Any())
            {
                return currentSummary;
            }

            var missingEnvmts = lastPeriodEnvmts.Where(i => missingEnvmtIds.Contains(i.Key)).ToList();
            var envUsageDetails = new Dictionary<string, EnvironmentUsageDetail>();
            foreach (var environment in missingEnvmts)
            {
                var envUsageDetail = environment.Value;
                if (envUsageDetail.EndState == DeletedEnvState)
                {
                    // Nothing to bill.
                    continue;
                }
                else if (envUsageDetail.EndState == AvailableEnvState)
                {
                    var billable = CalculateActiveVsoUnitsByTimeAndSku(start, end, environment.Value.Sku.Name);
                    var usageDetail = new EnvironmentUsageDetail
                    {
                        Name = envUsageDetail.Name,
                        EndState = AvailableEnvState,
                        Usage = new UsageDictionary
                        {
                            { "METER", billable },
                        },
                        Sku = environment.Value.Sku,
                        UserId = environment.Value.UserId,
                    };

                    // Update Environment list, Account billable units, and User billable units
                    currentSummary.UsageDetail.Environments.Add(environment.Key, usageDetail);
                    if (currentSummary.Usage.ContainsKey("METER"))
                    {
                        currentSummary.Usage["METER"] = currentSummary.Usage["METER"] + billable;
                    }
                    else
                    {
                        currentSummary.Usage.Add("METER", billable);
                    }

                    currentSummary.UsageDetail.Users = AggregateUserUsageDetails(currentSummary.UsageDetail.Users, billable, environment.Value.UserId);
                }
            }

            return currentSummary;
        }

        private Dictionary<string, UserUsageDetail> AggregateUserUsageDetails(IDictionary<string, UserUsageDetail> currentlist, double billable, string userId)
        {
            if (currentlist.TryGetValue(userId, out UserUsageDetail userUsageDetail))
            {
                var oldValue = 0.0d;
                userUsageDetail.Usage.TryGetValue("METER", out oldValue);
                userUsageDetail.Usage["METER"] = billable + oldValue;
            }
            else
            {
                currentlist.Add(userId, new UserUsageDetail
                {
                    Usage = new UsageDictionary { { "METER", billable } },
                });
            }

            return currentlist as Dictionary<string, UserUsageDetail>;
        }

        private decimal GetActiveVsoUnitsBySkuName(string name)
        {
            if (SkuDictionary.TryGetValue(name, out var sku))
            {
                return sku.GetActiveVsoUnitsPerHour();
            }
            else
            {
                // Sku is being used on an environment that does not
                // have a Sku Catelog record
                return 0.0m;
            }
        }

        private double CalculateActiveVsoUnitsByTimeAndSku(DateTime start, DateTime end, string sku)
        {
            var vsoUnitsPerHour = GetActiveVsoUnitsBySkuName(sku);
            const decimal secondsPerHour = 3600m;
            var vsoUnitsPerSecond = vsoUnitsPerHour / secondsPerHour;
            var totalSeconds = (decimal)end.Subtract(start).TotalSeconds;
            var vsoUnits = totalSeconds * vsoUnitsPerSecond;
            return (double)vsoUnits;
        }
    }
}
