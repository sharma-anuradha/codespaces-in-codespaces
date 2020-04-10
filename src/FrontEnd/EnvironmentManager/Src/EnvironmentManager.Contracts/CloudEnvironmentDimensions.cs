// <copyright file="CloudEnvironmentDimensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Aggregate dimensions over cloud environments.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CloudEnvironmentDimensions
    {
        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "skuName")]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the Azure location.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "location")]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the environment state.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "state")]
        public CloudEnvironmentState State { get; set; }

        /// <summary>
        /// Gets or sets the environment partener.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "partner")]
        public Partner? Partner { get; set; }

        /// <summary>
        /// Gets or sets the environment's iso country code.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "isoCountryCode")]
        public string IsoCountryCode { get; set; }

        /// <summary>
        /// Gets or sets the environment's azure geography.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "azureGeography")]
        public AzureGeography? AzureGeography { get; set; }
    }
}
