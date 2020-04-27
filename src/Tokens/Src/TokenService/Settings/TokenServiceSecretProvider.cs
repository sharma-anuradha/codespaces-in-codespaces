// <copyright file="TokenServiceSecretProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.TokenService.Settings
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class TokenServiceSecretProvider : CommonAppSecretsProvider
    {
        /// <summary>
        /// The application service principal client secret name.
        /// </summary>
        public const string TokenServicePrincipalClientSecretName = "TokenServicePrincipalClientSecret";

        /// <summary>
        /// Gets or sets the token service principal client secret.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string? TokenServicePrincipalClientSecret
        {
            get => TryGetSecret(TokenServicePrincipalClientSecretName, out var value) ? value : null;
            set => SetSecret(TokenServicePrincipalClientSecretName, value);
        }
    }
}
