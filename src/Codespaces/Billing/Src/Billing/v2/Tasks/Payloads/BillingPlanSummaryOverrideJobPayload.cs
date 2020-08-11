// <copyright file="BillingPlanSummaryOverrideJobPayload.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads
{
    /// <summary>
    /// Billing Plan Summary Override Job Payload.
    /// </summary>
    public class BillingPlanSummaryOverrideJobPayload
    {
        /// <summary>
        /// Gets or sets UTC time when the billing override started (on an hour boundary).
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets UTC time when the billing override ends (on an hour boundary).
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the override state.
        /// </summary>
        public BillingOverrideState BillingOverrideState { get; set; }

        /// <summary>
        /// Gets or sets the SKU that the billing override applies to. Optional as it could apply globally.
        /// </summary>
        public Sku Sku { get; set; }

        /// <summary>
        /// Gets or sets the priority of the override. A lower value indicates higher precedence.
        /// </summary>
        public long Priority { get; set; }
    }
}
