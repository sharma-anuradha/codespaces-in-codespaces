// <copyright file="ServicePrincipalSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings
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
        /// Gets or sets an Azure Keyvault secret identifier for the service principal client secret.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientSecretKeyVaultSecretIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the service principal tenant id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string TenantId { get; set; }
    }
}
