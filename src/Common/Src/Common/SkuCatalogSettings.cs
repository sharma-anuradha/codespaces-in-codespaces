// <copyright file="SkuCatalogSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The SKU catalog settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SkuCatalogSettings
    {
        /// <summary>
        /// Gets or sets the dictionary of Cloud Environment skus.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, SkuSettings> CloudEnvironmentSkuSettings { get; set; } = new Dictionary<string, SkuSettings>();

        /// <summary>
        /// Gets or sets the defaults for <see cref="CloudEnvironmentSkuSettings"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public SkuConfigurationSettings DefaultSkuConfiguration { get; set; } = new SkuConfigurationSettings();
    }
}
