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
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class BillingSummarySubmissionService : BillingServiceBase, IBillingSummarySubmissionService
    {
        // services
        private readonly IBillingEventManager billingEventManager;
        private readonly IBillingSubmissionCloudStorageFactory billingStorageFactory;

        /// <summary>
        /// Accoring to the FAQs - usage meters should arrive within 48 hours of when it was incurred.
        /// https://microsoft.sharepoint.com/teams/CustomerAcquisitionBilling/_layouts/15/WopiFrame.aspx?sourcedoc={c7a559f4-316d-46b1-b5d4-f52cdfbc4389}&action=edit&wd=target%28Onboarding.one%7C55f62e8d-ea91-4a90-982c-04899e106633%2FFAQ%7C25cc6e79-1e39-424d-9403-cd05d2f675e9%2F%29&wdorigin=703
        /// </summary>
        private readonly int lookBackThresholdHrs = 48;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummarySubmissionService"/> class.
        /// </summary>
        /// <param name="controlPlanInfo">Needed to find all available control plans</param>
        /// <param name="billingEventManager">used to get billing summaries and plan</param>
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
            : base(billingEventManager, controlPlanInfo, logger, claimedDistributedLease, taskHelper, "billingsub-worker")
        {

            this.billingEventManager = billingEventManager;
            this.billingStorageFactory = billingStorageFactory;
        }

        /// <inheritdoc />
        public async Task ProcessBillingSummariesAsync(CancellationToken cancellationToken)
        {
            await Execute(cancellationToken);
        }

        protected override async Task ExecuteInner(IDiagnosticsLogger childlogger, DateTime startTime, DateTime endTime, string planShard, AzureLocation region)
        {
            var batchID = Guid.NewGuid().ToString();
            bool addedEntries = false;
            var plans = await billingEventManager.GetPlansByShardAsync(
                                                    startTime,
                                                    endTime,
                                                    Logger,
                                                    new List<AzureLocation> { region },
                                                    planShard);
            Logger.LogInfo($"Submitting bill summaries for region {region} and shard {planShard}");
            foreach (var plan in plans)
            {
                Expression<Func<BillingEvent, bool>> filter = bev => bev.Plan == plan &&
                                                 startTime <= bev.Time &&
                                                 bev.Time < endTime &&
                                                 bev.Type == BillingEventTypes.BillingSummary
                                                 && ((BillingSummary)bev.Args).SubmissionState == BillingSubmissionState.None;
                var billingSummaries = await billingEventManager.GetPlanEventsAsync(filter, Logger);
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
                    Logger.LogInfo($"Billing Submissions for {region} and shard {planShard} were submitted to table, now submitting to queue");

                    // Get the storage mechanism for billing submission
                    var storageClient = await billingStorageFactory.CreateBillingSubmissionCloudStorage(region);
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

                    Logger.LogError($"Submitting queue message for batch {batchID} failed with exception:{ex.Message}");
                    throw;
                }
            }
        }

        private async Task<bool> ProcessSummary(BillingEvent billingEvent, string batchID)
        {
            var billingSummary = billingEvent.Args as BillingSummary;
            var addedEntries = false;
            var storageClient = await billingStorageFactory.CreateBillingSubmissionCloudStorage(billingEvent.Plan.Location);
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
                            Location = MapPav2Location(billingEvent.Plan.Location),
                            MeterID = meter,
                            SubscriptionId = billingEvent.Plan.Subscription,
                            ResourceUri = billingEvent.Plan.ResourceId,
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
                    await billingEventManager.UpdateEventAsync(billingEvent, Logger);
                }
                else
                {
                    // We don't want to submit this entry as it's zero quantity. Mark it and move on so that we don't look at it again
                    billingSummary.SubmissionState = BillingSubmissionState.NeverSubmit;
                    await billingEventManager.UpdateEventAsync(billingEvent, Logger);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to submit billing event for resourceID: {billingEvent.Plan.ResourceId}" + ex.Message);
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
