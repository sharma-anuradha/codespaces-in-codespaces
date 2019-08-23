// <copyright file="EnvironmentInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Accounts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    public class EnvironmentBillingInfo
    {
        /// <summary>
        /// Gets or sets the environment ID.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user-assigned name of the environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Cloud Environments (VSLS) profile ID of the user of the environment
        /// (not necessarily the account owner).
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the SKU of the cloud environment.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public Sku Sku { get; set; }
    }
}
