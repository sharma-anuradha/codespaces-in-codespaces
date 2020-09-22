// <copyright file="BillScrubberRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Tasks.Payloads
{
    /// <summary>
    /// Type which holds the request for scrubbing a plan.
    /// </summary>
    public class BillScrubberRequest : JobPayload
    {
        /// <summary>
        /// Gets or sets the plan ID we are trying to scrub.
        /// </summary>
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets the desired bill ending time.
        /// </summary>
        public DateTime DesiredEndTime { get; set; }

        /// <summary>
        /// Gets or sets whether to enable archiving
        /// </summary>
        public bool EnableArchiving { get; set; }

        /// <summary>
        /// Gets or sets whether to check for missing environments.
        /// </summary>
        public bool CheckForMissingEnvironments { get; set; }

        /// <summary>
        /// Gets or sets whether to check for final states.
        /// </summary>
        public bool CheckForFinalStates { get; set; }
    }
}
