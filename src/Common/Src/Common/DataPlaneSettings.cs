// <copyright file="DataPlaneSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Configuration settings for data plane mappings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class DataPlaneSettings
    {
        /// <summary>
        /// Gets or sets the default quotas for all data-plane subscriptions.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public AzureSubscriptionQuotaSettings DefaultQuotas { get; set; } = new AzureSubscriptionQuotaSettings();

        /// <summary>
        /// Gets or sets the default locations for all data-plane subscriptions.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public List<AzureLocation> DefaultLocations { get; set; } = new List<AzureLocation>();

        /// <summary>
        /// Gets or sets the data-plane subscriptions, indexed by subscription name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, AzureSubscriptionSettings> Subscriptions { get; set; } = new Dictionary<string, AzureSubscriptionSettings>();
    }
}
