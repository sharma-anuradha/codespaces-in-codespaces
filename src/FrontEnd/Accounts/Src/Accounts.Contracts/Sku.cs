// <copyright file="Sku.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Accounts
{
    /// <summary>
    /// Azure standard SKU model.
    /// </summary>
    public class Sku
    {
        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SKU tier.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "tier")]
        public string Tier { get; set; }
    }
}
