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
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly IControlPlaneInfo controlPlanInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="BillingSummarySubmissionService"/> class.
        /// </summary>
        /// <param name="controlPlanInfo">Needed to find all available control plans</param>
        /// <param name="billingEventManager">used to get billing summaries and plan</param>
        /// <param name="logger">the logger</param>
        /// <param name="billingStorageFactory">used to get billing storage collections</param>
        /// <param name="claimedDistributedLease"> the lease holder</param>
        /// <param name="taskHelper">the task helper</param>
        public BillingSummarySubmissionService(
            IControlPlaneInfo controlPlanInfo,
            IBillingEventManager billingEventManager,
            IDiagnosticsLogger logger,
            IBillingSubmissionCloudStorageFactory billingStorageFactory,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper)
            : base(billingEventManager, controlPlanInfo, logger, claimedDistributedLease, taskHelper, "billingsub_worker")
        {
            this.controlPlanInfo = controlPlanInfo;
            this.billingEventManager = billingEventManager;
            this.billingStorageFactory = billingStorageFactory;
            this.claimedDistributedLease = claimedDistributedLease;
        }

        /// <inheritdoc />
        public async Task ProcessBillingSummariesAsync(CancellationToken cancellationToken)
        {
            await Execute(cancellationToken);
        }

        protected override async Task ExecuteInner(IDiagnosticsLogger childlogger, DateTime startTime, DateTime endTime, string planShard, AzureLocation region)
        {
            Logger.AddValue("region", region.ToString());
            Logger.AddValue("planShard", planShard);
            await Logger.OperationScopeAsync(
                $"{ServiceName}_begin_submission",
                async (childLogger) =>
                {

                    var batchID = Guid.NewGuid().ToString();
                    bool addedEntries = false;
                    var plans = await billingEventManager.GetPlansByShardAsync(
                                                            startTime,
                                                            endTime,
                                                            Logger,
                                                            new List<AzureLocation> { region },
                                                            planShard);


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
                            await Logger.OperationScopeAsync(
                            $"{ServiceName}_begin_queue_submission",
                            async (childLogger2) =>
                                    {
                                        // Get the storage mechanism for billing submission
                                        var storageClient = await billingStorageFactory.CreateBillingSubmissionCloudStorage(region);
                                        var billingSummaryQueueSubmission = new BillingSummaryQueueSubmission()
                                        {
                                            BatchId = batchID,
                                            PartitionKey = batchID,
                                        };

                                        // Send off the queue submission
                                        await storageClient.PushBillingQueueSubmission(billingSummaryQueueSubmission);
                                    }, swallowException: true);
                        }
                        catch (Exception ex)
                        {

                            Logger.LogErrorWithDetail($"{ServiceName}_submission_error", ex.Message);
                            throw;
                        }
                    }
                }, swallowException: true);
        }

        /// <inheritdoc />
        public async Task CheckForBillingSubmissionErorrs(CancellationToken cancellationToken)
        {
            var regions = controlPlanInfo.GetAllDataPlaneLocations();
            foreach (var region in regions)
            {
                var leaseName = $"billsub_errorCheck_{region.ToString()}";
                using (var lease = await claimedDistributedLease.Obtain(
                                                $"{ServiceName}_leases",
                                                leaseName,
                                                TimeSpan.FromHours(1),
                                                Logger))
                {
                    if (lease != null)
                    {
                        await CheckForErrors(region);
                    }
                }
            }
        }

        private async Task CheckForErrors(AzureLocation region)
        {
            await Logger.OperationScopeAsync(
               $"{ServiceName}_CheckForErrors",
               async (childLogger) =>
               {
                   var errorStorage = await billingStorageFactory.CreateBillingSubmissionCloudStorage(region);
                   var errorsOnQueue = await errorStorage.CheckForErrorsOnQueue();

                   // If there's too many errors coming back, let's make sure we do not get stuck there. Let's watch the queue, alert and time out.
                   var maxErrorsProcessed = 0;
                   while (errorsOnQueue && maxErrorsProcessed < 5)
                   {
                       var errors = await errorStorage.GetSubmissionErrors();
                       foreach (var error in errors)
                       {
                           Logger.AddValue("error", error.Exception);
                           Logger.AddValue("errorMessage", error.Message);
                           Logger.AddValue("errorRowKey", error.UsageRecordRowKey);
                           Logger.AddValue("errorPartionKey", error.UsageRecordPartitionKey);
                           Logger.LogError("bill_submission_error");

                           try
                           {
                               // To make only single partition queries we need to make two queries. 
                               // One to the PA usage table to get our subscriptionID and one to the event manager to find the bill summary to mark in error.
                               var billUsageSubmission = await errorStorage.GetBillingTableSubmission(error.UsageRecordPartitionKey, error.UsageRecordRowKey);
                               Expression<Func<BillingEvent, bool>> filter = bev => bev.Plan.Subscription == billUsageSubmission.SubscriptionId &&
                                                                   bev.Type == BillingEventTypes.BillingSummary &&
                                                                   ((BillingSummary)bev.Args).EventId == error.UsageRecordRowKey;

                               var billSummary = (await billingEventManager.GetPlanEventsAsync(filter, Logger)).FirstOrDefault();
                               if (billSummary != null)
                               {
                                   // mark it as an error and resubmit it.
                                   ((BillingSummary)billSummary.Args).SubmissionState = BillingSubmissionState.Error;
                                   await billingEventManager.UpdateEventAsync(billSummary, Logger);
                               }
                               else
                               {
                                   Logger.AddValue("error", "We could not find the right record to mark as an error");
                                   Logger.AddValue("errorRowKey", error.UsageRecordRowKey);
                                   Logger.AddValue("errorPartionKey", error.UsageRecordPartitionKey);
                                   Logger.LogError("bill_submission_error");
                               }
                           }
                           catch (Exception e)
                           {

                               Logger.AddValue("error", "We found the error record but did not mark the bill summary sucessfully");
                               Logger.AddValue("errorRowKey", error.UsageRecordRowKey);
                               Logger.AddValue("errorPartionKey", error.UsageRecordPartitionKey);
                               Logger.LogError("bill_submission_error");
                           }

                       }
                       errorsOnQueue = await errorStorage.CheckForErrorsOnQueue();
                       ++maxErrorsProcessed;
                   }
               }, swallowException: true);
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
