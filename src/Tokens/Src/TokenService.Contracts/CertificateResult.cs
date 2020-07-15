// <copyright file="CertificateResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Result item from listing public certificates.
    /// </summary>
    /// <remarks>
    /// This class is designed to be compatible with the JSON Web Key (JWK) specification:
    /// https://tools.ietf.org/html/draft-ietf-jose-json-web-key-41 .
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CertificateResult
    {
        /// <summary>
        /// Gets or sets the issuer for which this certificate is used to sign tokens.
        /// </summary>
        /// <remarks>
        /// This value is omitted when certificates are requested for a single specific issuer.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Issuer { get; set; } = null;

        /// <summary>
        /// Gets or sets the key type. This is always "RSA" for certificates.
        /// </summary>
        [JsonProperty("kty")]
        public string KeyType
        {
            get => "RSA";
            set { }
        }

        /// <summary>
        /// Gets or sets the key ID. This is often, but not necessarily, the same as the thumbprint.
        /// </summary>
        [JsonProperty("kid", Required = Required.Always)]
        public string KeyId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        [JsonProperty("x5t", Required = Required.Always)]
        public string Thumbprint { get; set; } = null!;

        /// <summary>
        /// Gets or sets the base64-encoded public certificate bytes.
        /// </summary>
        /// <remarks>
        /// This is an array to accommodate a certificate chain. The first item in the array
        /// is the certificate for token validation; the others can be used to verify the first.
        /// </remarks>
        [JsonProperty("x5c", Required = Required.Always)]
        public string[] PublicCertificate { get; set; } = null!;

        /// <summary>
        /// Gets or sets the RSA key modulus, in Base64urlUInt encoding.
        /// </summary>
        [JsonProperty("n")]
        public string? Modulus { get; set; }

        /// <summary>
        /// Gets or sets the RSA key exponent, in Base64urlUInt encoding.
        /// </summary>
        [JsonProperty("e")]
        public string? Exponent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the primary certificate for the issuer.
        /// </summary>
        /// <remarks>
        /// The primary certificate corresponds to the private certificate currently used to sign
        /// tokens for this issuer. Non-primary certificates may have been primary in the past or
        /// may become primary in the future, so validators should also consider them when
        /// validating tokens from the issuer.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsPrimary { get; set; }
    }
}
