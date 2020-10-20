// <copyright file="SystemConfigurationMigrationSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common
{
    /// <summary>
    /// Migration Settings for SystemConfigurationRepository to use at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SystemConfigurationMigrationSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether migration should use global configurations collection instead of regional.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public bool UseGlobalConfigurationCollection { get; set; } = true;
    }
}
