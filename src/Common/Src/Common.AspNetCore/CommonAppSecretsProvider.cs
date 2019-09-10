// <copyright file="CommonAppSecretsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore
{
    /// <summary>
    /// Secrets that are passed in to the service as config at runtime.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CommonAppSecretsProvider : SecretProviderBase
    {
        /// <summary>
        /// The application servie principal client secret name.
        /// </summary>
        public const string AppServicePrincipalClientSecretName = "AppServicePrincipalClientSecret";

        /// <summary>
        /// Gets or sets the application service principal client secret.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string AppServicePrincipalClientSecret
        {
            get => TryGetSecret(AppServicePrincipalClientSecretName, out var value) ? value : null;
            set => SetSecret(AppServicePrincipalClientSecretName, value);
        }
    }
}
