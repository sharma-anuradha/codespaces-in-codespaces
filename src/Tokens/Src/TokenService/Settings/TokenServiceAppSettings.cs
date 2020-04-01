// <copyright file="TokenServiceAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenServiceAppSettings
    {
        /// <summary>
        /// Gets or sets the service principal settings for the token service.
        /// </summary>
        /// <remarks>
        /// The token service uses a separate SP from the other VSO services.
        /// </remarks>
        public ServicePrincipalSettings ServicePrincipal { get; set; } = null!;

        /// <summary>
        /// Gets or sets a mapping from issuer name to settings for the token issuer.
        /// </summary>
        public IDictionary<string, TokenIssuerSettings> IssuerSettings { get; set; } = null!;

        /// <summary>
        /// Gets or sets a mapping from issuer name to settings for the token issuer.
        /// </summary>
        public IDictionary<string, TokenAudienceSettings> AudienceSettings { get; set; } = null!;

        /// <summary>
        /// Gets or sets a mapping from client name to settings for the token client.
        /// </summary>
        public IDictionary<string, TokenClientSettings> ClientSettings { get; set; } = null!;

        /// <summary>
        /// Gets or sets settings for token exchange operations.
        /// </summary>
        public TokenExchangeSettings ExchangeSettings { get; set; } = null!;
    }
}
