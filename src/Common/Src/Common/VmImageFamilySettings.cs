// <copyright file="VmImageFamilySettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        /// Gets or sets the meaning image kind, which implies the meaning of <see cref="ImageName"/>.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public VmImageKind ImageKind { get; set; }
    }
}
