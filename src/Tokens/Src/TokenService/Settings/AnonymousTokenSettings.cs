// <copyright file="AnonymousTokenSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings for anonymous token issuer.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AnonymousTokenSettings
    {
        /// <summary>
        /// Gets or sets the issuer for anonymous tokens.
        /// </summary>
        /// <remarks>
        /// This is not an issuer URI, rather it is a key into the
        /// <see cref="TokenServiceAppSettings.IssuerSettings" /> dictionary.
        /// </remarks>
        public string Issuer { get; set; } = null!;

        /// <summary>
        /// Gets or sets the set of allowed audiences for anonymous tokens
        /// </summary>
        /// <remarks>
        /// Each value is a key into the <see cref="TokenServiceAppSettings.AudienceSettings" />
        /// dictionary. The first item is the default audience.
        /// </remarks>
        public string[] ValidAudiences { get; set; } = null!;

        /// <summary>
        /// Gets or sets the lifetime of generated anonymous tokens.
        /// </summary>
        /// <remarks>
        /// If unspecified, the issuer max lifetime is used.
        ///
        /// This must be specified in a form supported by TimeSpan.Parse(). Examples:
        ///   "0:15" = 15 minutes
        ///   "2:00" = 2 hours
        ///   "30" = 30 days
        /// Unfortunately ISO-8601 format is not supported:
        /// https://github.com/JamesNK/Newtonsoft.Json/issues/863
        /// https://github.com/dotnet/runtime/issues/28862
        /// .</remarks>
        public TimeSpan? Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the max length allowed for anonymous token display names
        /// </summary>
        /// <remarks>
        /// Requests with longer display names will be truncated.
        /// </remarks>
        public int DisplayNameMaxLength { get; set; } = 50;
    }
}
