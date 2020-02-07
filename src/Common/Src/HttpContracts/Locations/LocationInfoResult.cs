// <copyright file="LocationInfoResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts
{
    /// <summary>
    /// The location info REST API result.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class LocationInfoResult
    {
        /// <summary>
        /// Gets or sets a list of all SKUs available at the location.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public SkuInfoResult[] Skus { get; set; }

        /// <summary>
        /// Gets or sets the valid default auto suspend delay minutes.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int[] DefaultAutoSuspendDelayMinutes { get; set; }
    }
}
