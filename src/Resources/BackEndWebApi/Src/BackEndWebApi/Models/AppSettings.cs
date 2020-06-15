// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AppSettings : AppSettingsBase
    {
        /// <summary>
        /// Gets or sets resource-broker specific settings.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public BackEndAppSettings BackEnd { get; set; }
    }
}
