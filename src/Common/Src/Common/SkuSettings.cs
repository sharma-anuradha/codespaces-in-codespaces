// <copyright file="SkuSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
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
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Azure compute OS, either "Linux" or "Windows".
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public ComputeOS ComputeOS { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this SKU is available for creation.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the SKU tier.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public SkuTier Tier { get; set; }

        /// <summary>
        /// Gets or sets the number of VSO units per hour for storage.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public decimal StorageVsoUnitsPerHour { get; set; }

        /// <summary>
        /// Gets or sets the number of VSO units per hour for compute.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public decimal ComputeVsoUnitsPerHour { get; set; }

        /// <summary>
        /// Gets or sets the configuration settings for this SKU.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public SkuConfigurationSettings SkuConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the SKUs environments in this SKU may be migrated to.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public IEnumerable<string> SupportedSkuTransitions { get; set; }

        /// <summary>
        /// Gets or sets the features flags in the given SKU.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public IEnumerable<string> SupportedFeatureFlags { get; set; }

        /// <summary>
        /// Gets or sets the priority for the Sku. This is used for the display of skus in the clients.
        /// The lower the priority the higher it appers in the list of Skus.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public int Priority { get; set; }
    }
}
