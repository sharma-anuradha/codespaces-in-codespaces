// <copyright file="SkuConfigurationSettings.cs" company="Microsoft">
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
    public class SkuConfigurationSettings
    {
        /// <summary>
        /// Gets or sets the list of Azure locations.
        /// </summary>
        [JsonProperty(Required = Required.Default, ItemConverterType = typeof(StringEnumConverter))]
        public List<AzureLocation> Locations { get; set; } = new List<AzureLocation>();

        /// <summary>
        /// Gets or sets the pool size to be maintained, e.g., 25.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public int? ComputePoolSize { get; set; }

        /// <summary>
        /// Gets or sets the pool size to be maintained, e.g., 25.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public int? StoragePoolSize { get; set; }

        /// <summary>
        /// Gets or sets the compute image family.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ComputeImageFamily { get; set; }

        /// <summary>
        /// Gets or sets the storage image family.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string StorageImageFamily { get; set; }

        /// <summary>
        /// Gets or sets the VM agent image family.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string VmAgentImageFamily { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the tier is enabled.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; } = true;
    }
}
