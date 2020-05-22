// <copyright file="PlanSkuSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Settings for a plan SKU.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanSkuSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether this SKU is available for creation.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets key vault sku name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string KeyVaultSkuName { get; set; }

        /// <summary>
        /// Gets or sets the configuration settings for this SKU.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public PlanSkuConfigurationSettings PlanSkuConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the features flags in the given SKU.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public IEnumerable<string> SupportedFeatureFlags { get; set; }
    }
}
