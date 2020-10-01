// <copyright file="SystemStatusInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// System status information.
    /// </summary>
    public class SystemStatusInfo
    {
        /// <summary>
        /// Gets or sets the state of the update operation.
        /// </summary>
        [JsonProperty(PropertyName = "updateState")]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobState UpdateState { get; set; }

        /// <summary>
        /// Gets or sets the Visual Studio version number.
        /// </summary>
        [JsonProperty(PropertyName = "vsVersion")]
        [JsonConverter(typeof(VersionConverter))]
        public Version VsVersion { get; set; }
    }
}
