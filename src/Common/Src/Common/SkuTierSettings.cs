// <copyright file="SkuTierSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Settings for a cloud environment SKU.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuTierSettings
    {
        /// <summary>
        /// Gets or sets the Azure compute SKU family.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuFamily { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuName { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute SKU size.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuSize { get; set; }

        /// <summary>
        /// Gets or sets the number of compute vCPUs.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int ComputeSkuCores { get; set; }

        /// <summary>
        /// Gets or sets the storage size in GB.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int StorageSizeInGB { get; set; }

        /// <summary>
        /// Gets or sets the Azure storage SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StorageSkuName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tier is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; } = true;
    }
}
