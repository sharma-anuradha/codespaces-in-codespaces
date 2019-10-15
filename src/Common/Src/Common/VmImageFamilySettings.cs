// <copyright file="VmImageFamilySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The image family settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class VmImageFamilySettings : ImageFamilySettings
    {
        /// <summary>
        /// Gets or sets a value that indicates how <see cref="ImageFamilySettings.ImageName"/> is to be interpreted.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public VmImageKind ImageKind { get; set; }
    }
}
