// <copyright file="BillingSummaryRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads
{
    /// <summary>
    /// the Billing summary request.
    /// </summary>
    public class BillingSummaryRequest : JobPayload
    {
        /// <summary>
        /// Gets or sets The planId.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the desired bill ending time.
        /// </summary>
        public DateTime DesiredEndTime { get; set; }

        /// <summary>
        /// Gets or sets all relevant plan information used for submitting the bill.
        /// </summary>
        public VsoPlanInfo PlanInformation { get; set; }

        /// <summary>
        /// Gets or sets the list of billing overrides that apply to this plan.
        /// </summary>
        public IEnumerable<BillingPlanSummaryOverrideJobPayload> BillingOverrides { get; set; }

        /// <summary>
        /// Gets or sets the Partner <see cref="Partner"/>.
        /// </summary>
        public Partner? Partner { get; set; }

        /// <summary>
        /// Gets or sets whether to enable submission.
        /// </summary>
        public bool EnablePushAgentSubmission { get; set; }

        /// <summary>
        /// Gets or sets whether to enable partner submission.
        /// </summary>
        public bool EnablePartnerSubmission { get; set; }
    }
}
