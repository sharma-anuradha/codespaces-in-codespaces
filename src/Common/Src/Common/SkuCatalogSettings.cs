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

        /// <summary>
        /// Gets or sets the compute image families referenced in in <see cref="CloudEnvironmentSkuSettings"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, VmImageFamilySettings> ComputeImageFamilies { get; set; } = new Dictionary<string, VmImageFamilySettings>();

        /// <summary>
        /// Gets or sets the compute image families referenced in in <see cref="CloudEnvironmentSkuSettings"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, ImageFamilySettings> VmAgentImageFamilies { get; set; } = new Dictionary<string, ImageFamilySettings>();

        /// <summary>
        /// Gets or sets the storage image families referenced in in <see cref="CloudEnvironmentSkuSettings"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Dictionary<string, ImageFamilySettings> StorageImageFamilies { get; set; } = new Dictionary<string, ImageFamilySettings>();
    }
}
