// <copyright file="BillSummary.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Billing.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Database entity that represents an event that is important to track either
    /// directly for billing or for billing-related monitoring & consistency checking.
    /// </summary>
    public class BillSummary : CosmosDbEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BillSummary"/> class.
        /// </summary>
        public BillSummary()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BillSummary"/> class.
        /// </summary>
        /// <param name="id">The ID.</param>
        public BillSummary(Guid id)
        {
            Id = id.ToString();
        }

        /// <summary>
        /// Gets or sets the planId.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "planId")]
        public string PlanId { get; set; }

        /// <summary>
        /// Gets or sets UTC time when the billWasGenerated occurred.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "generatedTime")]
        public DateTime BillGenerationTime { get; set; }

        /// <summary>
        /// Gets or sets information about the plan that the  relates to.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "plan")]
        public VsoPlanInfo Plan { get; set; }

        /// <summary>
        /// Gets or sets UTC start of the period covered by this summary.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "periodStart")]
        public DateTime PeriodStart { get; set; }

        /// <summary>
        /// Gets or sets UTC end of the period covered by this summary.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "periodEnd")]
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the plan was deleted at the time that this billing summary was generated.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "planIsDeleted")]
        public bool PlanIsDeleted { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the final bill for the plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isFinalBill")]
        public bool IsFinalBill { get; set; }

        /// <summary>
        /// Gets or sets Total usage billed to the plan for the billing period for one or more
        /// billing meters, in each meter's units.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public IDictionary<string, double> Usage { get; set; }

        /// <summary>
        /// Gets or sets A more detailed breakdown of usage for the period. Supplemental information
        /// that does not contribute directly to the billed usage.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usageDetail")]
        public IList<EnvironmentUsage> UsageDetail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this billing data was emitted so that the customer will actually get
        /// billed for this usage.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "submissionState")]
        public BillingSubmissionState SubmissionState { get; set; }

        /// <summary>
        /// Creates the partition key used for this a BillSummary in an active (non-archive) table
        /// </summary>
        /// <param name="planId">BillSummary.PlanId</param>
        /// <returns>Partition key for active table</returns>
        public static string CreateActivePartitionKey(string planId)
        {
            return planId;
        }

        /// <summary>
        /// Creates the partition key used for this a BillSummary in an archive table
        /// </summary>
        /// <param name="planId">BillSummary.PlanId</param>
        /// <param name="billGenerationTime">BillSummary.BillGenerationTime</param>
        /// <returns>Partition key for archive table</returns>
        public static string CreateArchivedPartitionKey(string planId, DateTime billGenerationTime)
        {
            return $"{planId}_{billGenerationTime:yyyy_MM}";
        }
    }
}
