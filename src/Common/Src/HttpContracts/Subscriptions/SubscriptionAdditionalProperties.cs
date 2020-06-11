// <copyright file="SubscriptionAdditionalProperties.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// Subscription Notification additional properties.
    /// </summary>
    public class SubscriptionAdditionalProperties
    {
        /// <summary>
        /// Gets or sets resource provider properties.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "resourceProviderProperties")]
        public string ResourceProviderProperties { get; set; }

        /// <summary>
        /// Gets or sets resource provider properties.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "billingProperties")]
        public BillingProperties BillingProperties { get; set; }
    }
}
