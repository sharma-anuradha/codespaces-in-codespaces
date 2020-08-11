// <copyright file="BillingPlanCleanupProducer.cs" company="Microsoft">
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

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Defines a task which is designed to place jobs on the queue to process the billing summary for a plan.
    /// </summary>
    public class BillingPlanCleanupProducer : IBillingPlanCleanupProducer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillingPlanCleanupProducer"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">Target Job Queue Producer Factory.</param>
        public BillingPlanCleanupProducer(
            IJobQueueProducerFactory jobQueueProducerFactory)
        {
            JobQueueProducer = jobQueueProducerFactory.GetOrCreate(BillingLoggingConstants.BillingPlanCleanupQueue);
        }

        private IJobQueueProducer JobQueueProducer { get; }

        /// <inheritdoc/>
        public Task PublishJobAsync(
            string planId,
            DateTime desiredBillEndingTime,
            JobPayloadOptions jobPayloadOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken)
        {
            return logger.OperationScopeAsync(
                $"{BillingLoggingConstants.BillingPlanCleanupTask}_publish",
                async (childLogger) =>
                {
                    // Push job onto queue
                    await JobQueueProducer.AddJobAsync(new BillScrubberRequest() { PlanId = planId, DesiredEndTime = desiredBillEndingTime }, jobPayloadOptions, cancellationToken);
                });
        }
    }
}
