// <copyright file="TokenClientSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings for one token issuer.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenClientSettings
    {
        /// <summary>
        /// Gets or sets the possible AppIds used by this client. The client must authenticate with
        /// a token that proves one of the AppId identities.
        /// </summary>
        public string[] AppIds { get; set; } = null!;

#pragma warning disable SA1011 // Closing square brackets should be spaced correctly

        /// <summary>
        /// Gets or sets the set of issuers the client is allowed to use, or null if the client
        /// is not authorized to issue tokens.
        /// </summary>
        /// <remarks>
        /// Each value is a key into the <see cref="TokenServiceAppSettings.IssuerSettings" />
        /// dictionary.
        /// </remarks>
        public string[]? ValidIssuers { get; set; }

        /// <summary>
        /// Gets or sets the set of audiences the client is allowed to use, or null if the client
        /// is not authorized to issue tokens.
        /// </summary>
        /// <remarks>
        /// Each value is a key into the <see cref="TokenServiceAppSettings.AudienceSettings" />
        /// dictionary.
        /// </remarks>
        public string[]? ValidAudiences { get; set; }

#pragma warning restore SA1011 // Closing square brackets should be spaced correctly

        /// <summary>
        /// Gets or sets the display name of the client, that is filled in if the client does
        /// a token exchange with its (nameless) identitiy.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the email of the client, that is filled in if the client does a
        /// token exchange with its (email-less) identitiy.
        /// </summary>
        public string? Email { get; set; }
    }
}
