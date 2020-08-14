// <copyright file="BillingPlanSummaryProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Defines a task which is designed to place jobs on the queue to process the billing summary for a plan.
    /// </summary>
    public class BillingPlanSummaryProducer : IBillingPlanSummaryProducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanSummaryProducer"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">Target Job Queue Producer Factory.</param>
        public BillingPlanSummaryProducer(
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(BillingLoggingConstants.BillingPlanSummaryQueue);
        }

        private IJobQueueProducer JobQueueProducer { get; }

        /// <inheritdoc/>
        public Task PublishJobAsync(
            string planId,
            VsoPlanInfo planInfo,
            DateTime desiredBillEndingTime,
            Partner? partner,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> billingOverrides,
            JobPayloadOptions jobPayloadOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanSummaryTask}_publish",
                async (childLogger) =>
                {
                    // Push job onto queue
                    await JobQueueProducer.AddJobAsync(
                        new BillingSummaryRequest()
                        {
                            PlanId = planId,
                            BillingOverrides = billingOverrides,
                            DesiredEndTime = desiredBillEndingTime,
                            PlanInformation = planInfo,
                            Partner = partner,
                        },
                        jobPayloadOptions,
                        cancellationToken);
                });
        }
    }
}
