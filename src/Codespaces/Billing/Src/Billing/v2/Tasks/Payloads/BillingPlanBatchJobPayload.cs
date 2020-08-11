// <copyright file="BillingPlanBatchJobPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads
{
    /// <summary>
    /// Billing Plan Batch Job Payload.
    /// </summary>
    public class BillingPlanBatchJobPayload : JobPayload
    {
        /// <summary>
        /// Gets or sets the Plan Shard that should be targetted.
        /// </summary>
        public string PlanShard { get; set; }
    }
}
