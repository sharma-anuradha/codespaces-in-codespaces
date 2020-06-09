// <copyright file="AgentSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Agent settings.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AgentSettings
    {
        /// <summary>
        /// Gets or sets the last known good version of the agent which is compatible with the service.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string MinimumVersion { get; set; }
    }
}
