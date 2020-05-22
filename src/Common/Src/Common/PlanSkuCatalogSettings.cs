// <copyright file="PlanSkuCatalogSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The Plan SKU catalog settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class PlanSkuCatalogSettings
    {
        /// <summary>
        /// Gets or sets the default plan sku name.
        /// </summary>
        public string DefaultSkuName { get; set; }

        /// <summary>
        /// Gets or sets the dictionary of Cloud Environment skus.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, PlanSkuSettings> PlanSkuSettings { get; set; } = new Dictionary<string, PlanSkuSettings>();

        /// <summary>
        /// Gets or sets the defaults for <see cref="PlanSkuSettings"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, PlanSkuConfigurationSettings> DefaultPlanSkuConfiguration { get; set; } = new Dictionary<string, PlanSkuConfigurationSettings>();
    }
}
