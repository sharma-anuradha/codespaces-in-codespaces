// <copyright file="PartnerService.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// The service that pushes billing summeries into partner queues.
    /// </summary>
    public abstract class PartnerService : BillingServiceBase, IPartnerService
    {
        // services
        private readonly IBillingEventManager billingEventManager;
        private readonly IPartnerCloudStorageFactory gitHubStorageFactory;
        private readonly IClaimedDistributedLease claimedDistributedLease;
        private readonly IPlanManager planManager;
        private readonly IControlPlaneInfo controlPlanInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerService"/> class.
        /// </summary>
        /// <param name="controlPlanInfo">Needed to find all available control plans.</param>
        /// <param name="billingEventManager">used to get billing summaries and plan.</param>
        /// <param name="logger">the logger.</param>
        /// <param name="gitHubStorageFactory">used to get billing storage collections.</param>
        /// <param name="claimedDistributedLease"> the lease holder.</param>
        /// <param name="taskHelper">the task helper.</param>
        /// <param name="planManager">Used to get the list of plans to bill.</param>
        /// <param name="serviceName">The service name.</param>
        public PartnerService(
            IControlPlaneInfo controlPlanInfo,
            IBillingEventManager billingEventManager,
            IDiagnosticsLogger logger,
            IPartnerCloudStorageFactory gitHubStorageFactory,
            IClaimedDistributedLease claimedDistributedLease,
            ITaskHelper taskHelper,
            IPlanManager planManager)
            : base(controlPlanInfo, logger, claimedDistributedLease, taskHelper, planManager)
        {
            this.controlPlanInfo = controlPlanInfo;
            this.billingEventManager = billingEventManager;
            this.gitHubStorageFactory = gitHubStorageFactory;
            this.claimedDistributedLease = claimedDistributedLease;
            this.planManager = planManager;
        }

        /// <summary>
        /// Gets the id that disambiguates the partner storage account.
        /// </summary>
        protected abstract string PartnerId { get; }

        /// <summary>
        /// Gets the id that disambiguates the partner.
        /// </summary>
        protected abstract Partner Partner { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteInner(IDiagnosticsLogger logger, DateTime startTime, DateTime endTime, string planShard, AzureLocation region)
        {
            logger.AddValue("region", region.ToString());
            logger.AddValue("planShard", planShard);
            await logger.OperationScopeAsync(
                $"{ServiceName}_begin_submission",
                async (childLogger) =>
                {
                    var batchID = Guid.NewGuid().ToString();
                    var regions = new List<AzureLocation> { region };

                    var plans = await this.planManager.GetPartnerPlansByShardAsync(regions, planShard, Partner, childLogger);

                    foreach (var plan in plans)
                    {
                        await childLogger.OperationScopeAsync(
                            $"{ServiceName}_submission_plan",
                            async (innerLogger) =>
                            {
                                Expression<Func<BillingEvent, bool>> filter = x =>
                                x.Plan.Subscription == plan.Plan.Subscription &&
                                x.Plan.ResourceGroup == plan.Plan.ResourceGroup &&
                                x.Plan.Name == plan.Plan.Name &&
                                x.Plan.Location == plan.Plan.Location &&
                                x.Type == BillingEventTypes.BillingSummary &&
                                ((BillingSummary)x.Args).PartnerSubmissionState == BillingSubmissionState.None;

                                var billingSummaries = await this.billingEventManager.GetPlanEventsAsync(filter, innerLogger);

                                foreach (var summary in billingSummaries.OrderBy(o => o.Time))
                                {
                                    await ProcessSummary(summary, batchID);
                                }
                            }, swallowException: true);
                     }
                }, swallowException: true);
        }

        private async Task ProcessSummary(BillingEvent billingEvent, string batchID)
        {
            var billingSummary = billingEvent.Args as BillingSummary;
            var storageClient = await this.gitHubStorageFactory.CreatePartnerCloudStorage(billingEvent.Plan.Location, PartnerId);
            await Logger.OperationScopeAsync(
                $"{ServiceName}_processSummary",
                async (childLogger) =>
                {
                    var gitHubQueueSubmission = new PartnerQueueSubmission(billingEvent);

                    if (gitHubQueueSubmission.IsEmpty())
                    {
                        // Update the billing event to show it's empty and will never be submitted
                        billingSummary.PartnerSubmissionState = BillingSubmissionState.NeverSubmit;
                        await billingEventManager.UpdateEventAsync(billingEvent, Logger);
                        return;
                    }

                    // Update the billing event to show it's been submitted
                    billingSummary.PartnerSubmissionState = BillingSubmissionState.Submitted;
                    await billingEventManager.UpdateEventAsync(billingEvent, Logger);

                    // Submit all the stuff. An entry needs to be added to the table with the usage record
                    try
                    {
                        await storageClient.PushPartnerQueueSubmission(gitHubQueueSubmission);
                    }
                    catch (Exception)
                    {
                        // Update the billing event to show an error happend while pushing to the queue
                        billingSummary.PartnerSubmissionState = BillingSubmissionState.Error;
                        await billingEventManager.UpdateEventAsync(billingEvent, Logger);

                        throw;
                    }

                    childLogger.FluentAddValue("PartnerSummaryEndTime", billingSummary.PeriodEnd.ToString())
                        .FluentAddValue("PartnerId", PartnerId)
                        .FluentAddValue("ResourceId", billingEvent.Plan.ResourceId)
                        .FluentAddValue("Subscription", billingEvent.Plan.Subscription)
                        .FluentAddValue("ComputeTime", gitHubQueueSubmission.TotalComputeTime)
                        .FluentAddValue("StorageTime", gitHubQueueSubmission.TotalStorageTime)
                        .LogInfo("github_summary_submission");
                });
        }
    }
}
