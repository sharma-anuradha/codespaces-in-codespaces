// <copyright file="SkuInfoResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The SKU info REST API result.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuInfoResult
    {
        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SKU display name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the SKU OS.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "os")]
        public string OS { get; set; }
    }
}
