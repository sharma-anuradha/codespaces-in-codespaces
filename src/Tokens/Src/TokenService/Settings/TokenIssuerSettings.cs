// <copyright file="TokenIssuerSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings for one token issuer.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenIssuerSettings
    {
        /// <summary>
        /// Gets or sets the issuer URI.
        /// </summary>
        public string IssuerUri { get; set; } = null!;

        /// <summary>
        /// Gets or sets the issuer signing certificate name. The certificate will be
        /// retrieved by this name from the key vault in the app settings.
        /// </summary>
        /// <remarks>
        /// A public signing certificate can only support validating tokens; in that case
        /// <see cref="ValidateOnly"/> must be set to true. A private certificate
        /// can support both issuing and validating.
        /// </remarks>
        public string SigningCertificateName { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value indicating whether this issuer definition can only be used
        /// for validation.
        /// </summary>
        public bool ValidateOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum lifetime of tokens issued by this issuer.
        /// This is also the default if the expiration is unspecified in an issue request.
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
        public TimeSpan? MaxLifetime { get; set; }
    }
}
