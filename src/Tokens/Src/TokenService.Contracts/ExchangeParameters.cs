// <copyright file="ExchangeParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Contracts
{
    /// <summary>
    /// Request parameters for exchanging a token.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ExchangeParameters
    {
        /// <summary>
        /// Gets or sets the token to be exchanged, if that token was not specified in the
        /// `Authorization` request header.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the name of the token provider, if a <see cref="Token"/> was specified
        /// in the body.
        /// </summary>
        /// <remarks>
        /// Required if the <see cref="Token" /> property is specified. Must be one of the values
        /// from <see cref="ProviderNames" />.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Provider { get; set; }

        /// <summary>
        /// Gets or sets the optional requested audience for the resulting token.
        /// </summary>
        /// <remarks>If unspecified, the configured default audience will be used.</remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Audience { get; set; }

        /// <summary>
        /// Gets or sets the optional requested lifetime for the resulting token.
        /// </summary>
        /// <remarks>
        /// If the requested lifetime is greater than the configured maximum, the maximum is used.
        /// If unspecified, the configured default lifetime will be used.
        /// </remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TimeSpan? Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the optional requested scopes for the resulting token.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[]? Scopes { get; set; }
    }
}
