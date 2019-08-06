// <copyright file="SkuCatalogSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
{
    /// <summary>
    /// The SKU catalog settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuCatalogSettings
    {
        /// <summary>
        /// Gets the list of Cloud Environment skus.
        /// </summary>
        [JsonProperty]
        public List<CloudEnvironmentSkuSettings> CloudEnvironmentSkuSettings { get; } = new List<CloudEnvironmentSkuSettings>();

        /// <summary>
        /// Gets the list of default Azure locations.
        /// </summary>
        [JsonProperty]
        public List<AzureLocation> DefaultLocations { get; } = new List<AzureLocation>();

        /// <summary>
        /// Gets a mapping of OS type to default VM image for that OS.
        /// </summary>
        [JsonProperty]
        public Dictionary<ComputeOS, string> DefaultVMImages { get; } = new Dictionary<ComputeOS, string>();
    }
}
