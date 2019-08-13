﻿// <copyright file="CloudEnvironmentSkuSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
{
    /// <summary>
    /// Settings for a cloud environment SKU.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CloudEnvironmentSkuSettings
    {
        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the SKU display name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SkuDisplayName { get; set; }

        /// <summary>
        /// Gets the list of Azure locations, overriding <see cref="SkuCatalogSettings.DefaultLocations"/>.
        /// </summary>
        [JsonProperty]
        public List<AzureLocation> OverrideLocations { get; } = new List<AzureLocation>();

        /// <summary>
        /// Gets or sets the pool level that we would like to be maintained, e.g., 25.
        /// </summary>
        [JsonProperty]
        public int? OverridePoolLevel { get; set; }

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
    }
}
