using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class BillingSummarySubmissionService : IBillingSummarySubmissionService
    {
        // services
        private readonly IControlPlaneInfo controlPlanInfo;
        private readonly IBillingEventManager billingEventManager;
        private readonly IDiagnosticsLogger logger;
        private readonly IBillingSubmissionCloudStorageFactory billingStorageFactory;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly ITaskHelper taskHelper;
        private readonly string billingSubmissionWorkerLogBase = "billingsubmission-worker";

        /// <summary>
        /// Accoring to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
        /// https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/WopiFrame.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Onboarding.one%7C55f62e8d-ea91-4a90-982c-04899e106633%2FFAQ%7C25cc6e79-1e39-424d-9403-cd05d2f675e9%2F%29&wdorigin=703
        /// </summary>
        private readonly int lookBackThresholdHrs = 48;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummarySubmissionService"/> class.
        /// </summary>
        /// <param name="controlPlanInfo">Needed to find all available control plans</param>
        /// <param name="billingEventManager">used to get billing summaries and accounts</param>
        /// <param name="logger">the logger</param>
        /// <param name="billingStorageFactory">used to get billing storage collections</param>
        /// <param name="claimedDistributedLease"></param>
        /// <param name="taskHelper"></param>
        public BillingSummarySubmissionService(
            IControlPlaneInfo controlPlanInfo,
            IBillingEventManager billingEventManager,
            IDiagnosticsLogger logger,
            IBillingSubmissionCloudStorageFactory billingStorageFactory,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper)
        {
            this.controlPlanInfo = controlPlanInfo;
            this.billingEventManager = billingEventManager;
            this.logger = logger;
            this.billingStorageFactory = billingStorageFactory;
            this.claimedDistributedLease = claimedDistributedLease;
            this.taskHelper = taskHelper;
        }

        /// <inheritdoc />
        public async Task ProcessBillingSummaries()
        {

            var accountShards = new List<string> { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" }.Shuffle();

            var controlledRegions = controlPlanInfo.GetAllDataPlaneLocations().Shuffle();
            var accountsToRegions = accountShards.SelectMany(x => controlledRegions, (accoundShard, region) => new { accoundShard, region });

            var startTime = DateTime.UtcNow.Subtract(TimeSpan.FromHours(lookBackThresholdHrs));
            var endTime = DateTime.UtcNow;

            logger.FluentAddBaseValue("startCalculationTime", startTime);
            logger.FluentAddBaseValue("endCalculationTime", endTime);

            // TODO: There's a lot here that's similar to the Billing Worker. If we need another similar worker, perhaps we should refactor this into an abstract BillingWorker that skeletons out the inner "what to do with accounts" aspect.
            await taskHelper.RunBackgroundEnumerableAsync(
                $"{billingSubmissionWorkerLogBase}-run",
                accountsToRegions,
                async (x, childlogger) =>
                {
                    var leaseName = $"billingsubmissionworkerrun-{x.accoundShard}-{x.region}";
                    var batchID = Guid.NewGuid().ToString();
                    childlogger.FluentAddBaseValue("leaseName", leaseName);
                    using (var lease = await claimedDistributedLease.Obtain($"{billingSubmissionWorkerLogBase}-leases", leaseName, TimeSpan.FromHours(1), childlogger))
                    {
                        if (lease != null)
                        {
                            bool addedEntries = false;
                            var accounts = await billingEventManager.GetAccountsByShardAsync(
                                                                    startTime,
                                                                    endTime,
                                                                    logger,
                                                                    new List<AzureLocation> { x.region },
                                                                    x.accoundShard);
                            logger.LogInfo($"Submitting bill summaries for region {x.region} and shard {x.accoundShard}");
                            foreach (var account in accounts)
                            {
                                Expression<Func<BillingEvent, bool>> filter = bev => bev.Account == account &&
                                                                 startTime <= bev.Time &&
                                                                 bev.Time < endTime &&
                                                                 bev.Type == BillingEventTypes.BillingSummary
                                                                 && ((BillingSummary)bev.Args).SubmissionState == BillingSubmissionState.None;
                                var billingSummaries = await billingEventManager.GetAccountEventsAsync(filter, logger);
                                foreach (var summary in billingSummaries)
                                {
                                    addedEntries |= await ProcessSummary(summary, batchID);
                                }
                            }

                            // If we've pushed anything, now we should create a queue entry to record the whole batch.
                            if (addedEntries)
                            {
                                try
                                {
                                    logger.LogInfo($"Billing Submissions for {x.region} and shard {x.accoundShard} were submitted to table, now submitting to queue");

                                    // Get the storage mechanism for billing submission
                                    var storageClient = await billingStorageFactory.CreateBillingSubmissionCloudStorage(x.region);
                                    var billingSummaryQueueSubmission = new BillingSummaryQueueSubmission()
                                    {
                                        BatchId = batchID,
                                        PartitionKey = batchID,
                                    };

                                    // Send off the queue submission
                                    await storageClient.PushBillingQueueSubmission(billingSummaryQueueSubmission);
                                }
                                catch (Exception ex)
                                {

                                    logger.LogError($"Submitting queue message for batch {batchID} failed with exception:{ex.Message}");
                                    throw;
                                }
                            }

                        }
                    }
                },
                logger);
        }

        private async Task<bool> ProcessSummary(BillingEvent billingEvent, string batchID)
        {
            var billingSummary = billingEvent.Args as BillingSummary;
            var addedEntries = false;
            var storageClient = await billingStorageFactory.CreateBillingSubmissionCloudStorage(billingEvent.Account.Location);
            var eventID = Guid.NewGuid();
            try
            {
                foreach (var meter in billingSummary.Usage.Keys)
                {
                    var quantity = billingSummary.Usage[meter];
                    if (quantity > 0)
                    {
                        // One submission per meter.
                        var billingSummaryTableSubmission = new BillingSummaryTableSubmission(batchID, eventID.ToString())
                        {
                            EventDateTime = billingSummary.PeriodEnd,
                            // TODO: EventID needs to have some other correlation to it for the case of multiple meters.
                            EventId = eventID,
                            Location = MapPav2Location(billingEvent.Account.Location),
                            MeterID = "3bd29058-3028-448d-8ba4-f8f60b731019", //"5f3afa79-01ad-4d7e-b691-73feca4ea350", // eventually just use meter here.
                            SubscriptionId = billingEvent.Account.Subscription,
                            ResourceUri = billingEvent.Account.ResourceId,
                            Quantity = billingSummary.Usage[meter],
                        };

                        // Submit all the stuff. An entry needs to be added to the table with the usage record
                        await storageClient.InsertOrUpdateBillingTableSubmission(billingSummaryTableSubmission);
                        addedEntries = true;
                    }
                }

                // Check if we added anything. If so, mark the billing record as appropriate
                if (addedEntries)
                {
                    // Update the billing event to show it's been submitted
                    billingSummary.SubmissionState = BillingSubmissionState.Submitted;
                    billingSummary.EventId = eventID.ToString();
                    await billingEventManager.UpdateEventAsync(billingEvent, logger);
                }
                else
                {
                    // We don't want to submit this entry as it's zero quantity. Mark it and move on so that we don't look at it again
                    billingSummary.SubmissionState = BillingSubmissionState.NeverSubmit;
                    await billingEventManager.UpdateEventAsync(billingEvent, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to submit billing event for resourceID: {billingEvent.Account.ResourceId}" + ex.Message);
                throw;
            }

            return addedEntries;
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
