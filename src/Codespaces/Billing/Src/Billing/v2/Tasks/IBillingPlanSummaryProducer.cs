// <copyright file="IBillingPlanSummaryProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks
{
    /// <summary>
    /// Defines a task which is designed to place jobs on the queue to produce a billing summary.
    /// </summary>
    public interface IBillingPlanSummaryProducer
    {
        /// <summary>
        /// Publish job of Billing Plan Batch queue.
        /// </summary>
        /// <param name="planId">Target Plan Id.</param>
        /// <param name="planInfo">Target Plan Info.</param>
        /// <param name="desiredEndTime">The desired bill ending time.</param>
        /// <param name="billingOverrides">Target Billing Overrides.</param>
        /// <param name="jobPayloadOptions">Target Job Payload Options.</param>
        /// <param name="logger">Target Logger.</param>
        /// <param name="cancellationToken">Target Cancellation Token.</param>
        /// <param name="partner">Target Partner.</param>
        /// <returns>Running task.</returns>
        Task PublishJobAsync(
            string planId,
            VsoPlanInfo planInfo,
            DateTime desiredEndTime,
            IEnumerable<BillingPlanSummaryOverrideJobPayload> billingOverrides,
            JobPayloadOptions jobPayloadOptions,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken,
            Partner? partner = null);
    }
}
