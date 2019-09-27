// <copyright file="ImageFamilySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The image family settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ImageFamilySettings
    {
        /// <summary>
        /// Gets or sets the image name.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ImageName { get; set; }

        /// <summary>
        /// Gets or sets the image version.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ImageVersion { get; set; }
    }
}
