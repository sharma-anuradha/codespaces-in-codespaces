// <copyright file="AzureSubscriptionQuotaSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Azure subscription quota settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AzureSubscriptionQuotaSettings
    {
        /// <summary>
        /// Gets or sets the compute quotas.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, int> Compute { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the storage quotas.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, int> Storage { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets or sets the network quotas.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, int> Network { get; set; } = new Dictionary<string, int>();
    }
}
