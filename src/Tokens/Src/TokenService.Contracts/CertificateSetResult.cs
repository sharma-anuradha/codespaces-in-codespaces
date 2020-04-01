// <copyright file="CertificateSetResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Result from listing public certificates.
    /// </summary>
    /// <remarks>
    /// This class is designed to be compatible with the JSON Web Key Set (JWKS) specification:
    /// https://tools.ietf.org/html/draft-ietf-jose-json-web-key-41 .
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CertificateSetResult
    {
        /// <summary>
        /// Gets or sets the set of certificates.
        /// </summary>
        [JsonProperty("keys", Required = Required.Always)]
        public CertificateResult[] Certificates { get; set; } = null!;
    }
}
