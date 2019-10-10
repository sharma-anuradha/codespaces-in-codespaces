// <copyright file="SkuInfoResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The SKU info REST API result.
    /// </summary>
    public class SkuInfoResult
    {
        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the SKU display name.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the SKU OS.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "os")]
        public string OS { get; set; }
    }
}
