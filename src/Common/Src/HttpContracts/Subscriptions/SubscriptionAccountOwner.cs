// <copyright file="SubscriptionAccountOwner.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Subscriptions
{
    /// <summary>
    /// Subscription Account Owner.
    /// </summary>
    public class SubscriptionAccountOwner
    {
        /// <summary>
        /// Gets or sets Account Owners Puid.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "puid")]
        public string Puid { get; set; }

        /// <summary>
        /// Gets or sets Account Owners Email.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "email")]
        public string Email { get; set; }
    }
}
