// <copyright file="PartnerQueueSubmission.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Contract for submission.
    /// </summary>
    public class PartnerQueueSubmission
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartnerQueueSubmission"/> class.
        /// </summary>
        /// <param name="billingEvent">The billing event being pushed to a partner.</param>
        public PartnerQueueSubmission(BillingEvent billingEvent)
        {
            Id = billingEvent.Id;
            Plan = new PartnerPlan(billingEvent.Plan);
            Time = DateTime.UtcNow;

            var summary = billingEvent.Args as BillingSummary;
            PeriodStart = summary.PeriodStart;
            PeriodEnd = summary.PeriodEnd;

            if (summary.UsageDetail != null)
            {
                UsageDetail = new PartnerUsageDetail(summary.UsageDetail);
            }
        }

        /// <summary>
        /// Gets or sets UTC time this record is created for debugging purposes.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "time")]
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the id used to match the submission IDs.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the partner plan.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "plan")]
        public PartnerPlan Plan { get; set; }

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
        /// Gets or sets a more detailed breakdown of usage for the period. Supplemental information
        /// that does not contribute directly to the billed usage.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usageDetail")]
        public PartnerUsageDetail UsageDetail { get; set; }

        /// <summary>
        /// Gets the total compute time.
        /// </summary>
        /// <returns>Returns total compute time.</returns>
        public double TotalComputeTime => this?.UsageDetail?.Environments?.Sum(o => o?.TotalComputeTime) ?? 0;

        /// <summary>
        /// Gets the total storage time.
        /// </summary>
        /// <returns>Returns total storage time.</returns>
        public double TotalStorageTime => this?.UsageDetail?.Environments?.Sum(o => o?.TotalStorageTime) ?? 0;

        /// <summary>
        /// Returns if the detail has any actual data.
        /// </summary>
        /// <returns>Returns true or false.</returns>
        public bool IsEmpty() => TotalComputeTime == 0 && TotalStorageTime == 0;

        /// <summary>
        /// Convert to Json.
        /// </summary>
        /// <returns>Returns json.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
