// <copyright file="SkuSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Settings for a cloud environment SKU.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuSettings
    {
        /// <summary>
        /// Gets or sets the SKU display name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SkuDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute SKU family, e.g., "standardFSv2Family".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuFamily { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute SKU name, e.g., "Standard_F4s_v2".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuName { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute SKU size, e.g., "F4s_v2".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ComputeSkuSize { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute OS, either "Linux" or "Windows".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets the Azure storage SKU name: Premium_LRS, Premium_ZRS, Standard_GRS, Standard_LRS, Standard_RAGRS, or Standard_ZRS.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string StorageSkuName { get; set; }

        /// <summary>
        /// Gets or sets the requested file storage size in GB.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int StorageSizeInGB { get; set; }

        /// <summary>
        /// Gets or sets the number of Cloud Environment Units that will be billed for this SKU when storage is active.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public decimal StorageCloudEnvironmentUnits { get; set; }

        /// <summary>
        /// Gets or sets the number of Cloud Environment Units that will be billed for this SKU when compute is active.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public decimal ComputeCloudEnvironmentUnits { get; set; }

        /// <summary>
        /// Gets or sets the configuration settings for this SKU.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public SkuConfigurationSettings SkuConfiguration { get; set; }
    }
}
