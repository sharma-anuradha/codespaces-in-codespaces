// <copyright file="TokenAudienceSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings for one token audience.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenAudienceSettings
    {
        /// <summary>
        /// Gets or sets the audience URI.
        /// </summary>
        public string AudienceUri { get; set; } = null!;

        /// <summary>
        /// Gets or sets the optional audience encrypting certificate name, or null if tokens
        /// for the audience are not encrypted. The certificate will be retrieved
        /// by this name from the key vault in the app settings.
        /// </summary>
        /// <remarks>
        /// A public encrypting certificate can only support issuing tokens; in that case
        /// <see cref="IssueOnly"/> must be set to true. A private certificate
        /// can support both issuing and validating.
        /// </remarks>
        public string? EncryptingCertificateName { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether this audience definition can only be used
        /// for issuing.
        /// </summary>
        public bool IssueOnly { get; set; } = false;
    }
}
