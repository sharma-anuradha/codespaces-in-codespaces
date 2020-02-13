// <copyright file="TokenSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A settings object for certifcates used to token signing and encryption.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenSettings
    {
        /// <summary>
        /// Gets or sets the name of the certificate used for signing.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string IssuerCertificateName { get; set; }

        /// <summary>
        /// Gets or sets the name of the certificate used for encryption.  If not specified, encryption is not performed.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string AudienceCertificateName { get; set; } = null;

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
        /// Gets or sets the name of KeyVault storing the certificates.  If not set, will use the environment's default KeyVault.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string KeyVaultName { get; set; }

        /// <summary>
        /// Gets or sets the maximum lifetime of issued tokens.
        /// </summary>
        /// <remarks>
        /// This must be specified in a form supported by TimeSpan.Parse(). Examples:
        ///   "0:15" = 15 minutes
        ///   "2:00" = 2 hours
        ///   "30" = 30 days
        /// Unfortunately ISO-8601 format is not supported:
        /// https://github.com/JamesNK/Newtonsoft.Json/issues/863
        /// https://github.com/dotnet/runtime/issues/28862
        /// .</remarks>
        [JsonProperty(Required = Required.Default)]
        public TimeSpan? Lifetime { get; set; }
    }
}
