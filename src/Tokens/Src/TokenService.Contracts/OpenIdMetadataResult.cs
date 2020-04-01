// <copyright file="OpenIdMetadataResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Result from a request for OpenId Connect provider metadata.
    /// </summary>
    /// <remarks>
    /// Implements the minimum required properties according to the spec:
    /// https://openid.net/specs/openid-connect-discovery-1_0.html .
    /// </remarks>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class OpenIdMetadataResult
    {
        /// <summary>
        /// Gets or sets the issuer.
        /// </summary>
        [JsonProperty("issuer", Required = Required.Always)]
        public string Issuer { get; set; } = null!;

        /// <summary>
        /// Gets or sets the authorization endpoint.
        /// </summary>
        [JsonProperty("authorization_endpoint", Required = Required.Always)]
        public string AuthorizationEndpoint { get; set; } = null!;

        /// <summary>
        /// Gets or sets the keys endpoint.
        /// </summary>
        [JsonProperty("jwks_uri", Required = Required.Always)]
        public string KeysEndpoint { get; set; } = null!;

        /// <summary>
        /// Gets or sets the list of OAuth 2.0 response types supported.
        /// </summary>
        [JsonProperty("response_types_supported", Required = Required.Always)]
        public string[] ResponseTypesSupported { get; set; } = null!;

        /// <summary>
        /// Gets or sets the list of subject types supported.
        /// </summary>
        [JsonProperty("subject_types_supported", Required = Required.Always)]
        public string[] SubjectTypesSupported { get; set; } = null!;

        /// <summary>
        /// Gets or sets the list of token-signing algorithms supported.
        /// </summary>
        [JsonProperty("id_token_signing_alg_values_supported", Required = Required.Always)]
        public string[] TokenSigningAlgorithmsSupported { get; set; } = null!;
    }
}
