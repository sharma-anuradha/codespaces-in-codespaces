// <copyright file="CertificateSettings.cs" company="Microsoft">
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
    public class CertificateSettings
    {
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
    }
}
