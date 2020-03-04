// <copyright file="KeyVaultSecretProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// Secret provider using Managed Identity and environment key vault.
    /// </summary>
    public class KeyVaultSecretProvider : ISecretProvider
    {
        private readonly IControlPlaneInfo controlPlaneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecretProvider"/> class.
        /// </summary>
        /// <param name="controlPlaneInfo">the control plane info.</param>
        public KeyVaultSecretProvider(IControlPlaneInfo controlPlaneInfo)
        {
            this.controlPlaneInfo = controlPlaneInfo;
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string secretName)
        {
            if (secretName?.Equals("AppServicePrincipalClientSecret") == true)
            {
                var keyVaultName = controlPlaneInfo.EnvironmentKeyVaultName;
                const string keyVaultSecretName = "app-sp-password";
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var kv = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                var password = await kv.GetSecretAsync($"https://{keyVaultName}.vault.azure.net/secrets/{keyVaultSecretName}");
                return password.Value;
            }

            throw new InvalidOperationException($"Secret not found: {secretName}");
        }
    }
}
