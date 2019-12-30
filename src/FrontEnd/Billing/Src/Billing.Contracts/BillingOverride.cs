// <copyright file="BillingOverride.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Contains details about a billing override.
    /// </summary>
    public class BillingOverride : TaggedEntity
    {
        /// <summary>
        /// Gets or sets UTC time when the billing override started (on an hour boundary).
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "startTime")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets UTC time when the billing override ends (on an hour boundary).
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "endTime")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the ID of the subscription that the billing override applies to.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull, PropertyName = "subscription")]
        public string Subscription { get; set; }

        /// <summary>
        /// Gets or sets the SkuPlan the billing override applies to.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull, PropertyName = "plan")]
        public VsoPlanInfo Plan { get; set; }

        /// <summary>
        /// Gets or sets the override state.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "billingOverrideState")]
        public BillingOverrideState BillingOverrideState { get; set; }

        /// <summary>
        /// Gets or sets the SKU that the billing override applies to. Optional as it could apply globally.
        /// </summary>
        [JsonProperty(Required = Required.AllowNull, PropertyName = "sku")]
        public Sku Sku { get; set; }

        /// <summary>
        /// Gets or sets the priority of the override. A lower value indicates higher precedence.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "priority")]
        public long Priority { get; set; }
    }
}