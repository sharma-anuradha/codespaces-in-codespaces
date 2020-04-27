// <copyright file="AuthenticationSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A settings object for certificates and tokens.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class AuthenticationSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the token service should be called for
        /// issuing tokens. If false, tokens are issued directly by this service.
        /// </summary>
        public bool UseTokenService { get; set; } = true;

        /// <summary>
        /// Gets or sets the base address for the tokens api. Only applicable
        /// if <see cref="UseTokenService" /> is true.
        /// </summary>
        public string TokenServiceBaseAddress { get; set; }

        /// <summary>
        /// Gets or sets the interval for token certificate refreshes.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public TimeSpan CertificateRefreshInterval { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Gets or sets the settings for VM tokens.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public TokenSettings VmTokenSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for VS SaaS tokens.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public TokenSettings VsSaaSTokenSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for environment connection tokens.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public TokenSettings ConnectionTokenSettings { get; set; }
    }
}
