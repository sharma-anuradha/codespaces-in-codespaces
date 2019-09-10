// <copyright file="ServicePrincipalSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A settings object for an Azure Service Principal.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ServicePrincipalSettings
    {
        /// <summary>
        /// Gets or sets the service principal client id (aka appid).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets serivce principal client secret name for use with ISecretProvider.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientSecretName { get; set; }

        /// <summary>
        /// Gets or sets the service principal tenant id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string TenantId { get; set; }
    }
}
