// <copyright file="PlanSkuConfigurationSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Settings for a cloud environment SKU.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanSkuConfigurationSettings
    {
        /// <summary>
        /// Gets or sets the list of Azure locations.
        /// </summary>
        [JsonProperty(Required = Required.Default, ItemConverterType = typeof(StringEnumConverter))]
        public List<AzureLocation> Locations { get; set; } = new List<AzureLocation>();

        /// <summary>
        /// Gets or sets the pool size to be maintained, e.g., 10.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public int? KeyVaultPoolSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the sku configuration is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; } = true;
    }
}
