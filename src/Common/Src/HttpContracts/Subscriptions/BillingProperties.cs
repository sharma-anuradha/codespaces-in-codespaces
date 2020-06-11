// <copyright file="BillingProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// JSON body properties for Billing properties.
    /// </summary>
    public class BillingProperties
    {
        /// <summary>
        /// Gets or sets Channel type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "channelType")]
        public string ChannelType { get; set; }

        /// <summary>
        /// Gets or sets Payment type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "paymentType")]
        public string PaymentType { get; set; }

        /// <summary>
        /// Gets or sets Workload type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "workloadType")]
        public string WorkloadType { get; set; }

        /// <summary>
        /// Gets or sets Billing type.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "billingType")]
        public string BillingType { get; set; }

        /// <summary>
        /// Gets or sets Tier.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tier")]
        public string Tier { get; set; }
    }
}