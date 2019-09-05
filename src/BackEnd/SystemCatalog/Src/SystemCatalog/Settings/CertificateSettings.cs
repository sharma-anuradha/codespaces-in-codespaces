// <copyright file="CertificateSettings.cs" company="Microsoft">
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
    public class CertificateSettings
    {
        /// <summary>
        /// Gets or sets the subscription id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets an resource group name of the keyvault.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Gets or sets the name of the key vault.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string KeyValutName { get; set; }

        /// <summary>
        /// Gets or sets the name of the certificate.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string CertificateName { get; set; }

        /// <summary>
        /// Gets or sets the issuer of the certificates.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the audience of the tokens.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Audience { get; set; }

        /// <summary>
        /// Gets or sets the Client Id (Appid).
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the key vault secret identifier.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ClientSecretKeyVaultSecretIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the tenant id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string TenantId { get; set; }
    }
}
