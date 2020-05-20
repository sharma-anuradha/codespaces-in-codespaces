// <copyright file="KeyVaultSecretsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// Client to perform key vault secret operations.
    /// </summary>
    [LoggingBaseName(LoggingBaseName)]
    public class KeyVaultSecretsProvider
    {
        private const string LoggingBaseName = "keyvault_secrets_provider";
        private IKeyVaultClient cachedKeyVaultClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecretsProvider"/> class.
        /// </summary>
        /// <param name="servicePrincipal">The service principal.</param>
        /// <param name="keyVaultName">The key vault name.</param>
        public KeyVaultSecretsProvider(IServicePrincipal servicePrincipal, string keyVaultName)
        {
            ServicePrincipal = servicePrincipal;
            KeyVaultName = keyVaultName;
        }

        private IServicePrincipal ServicePrincipal { get; }

        private string KeyVaultName { get; }

        private string KeyvaultBaseUrl => $"https://{KeyVaultName}.vault.azure.net/";

        /// <summary>
        /// Get a secret value by secret name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The secret value.</returns>
        public async Task<string> GetSecretAsync(string secretName, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync($"{LoggingBaseName}_get_secret", async childLogger =>
            {
                childLogger
                    .FluentAddValue("KeyVaultName", KeyVaultName)
                    .FluentAddValue("SecretName", secretName);

                Requires.NotNullOrEmpty(secretName, nameof(secretName));

                var keyVaultClient = GetKeyVaultClient();
                var secretBundle = await keyVaultClient.GetSecretAsync(KeyvaultBaseUrl, secretName);

                // Managed secrets are always enabled and does not expire.
                // TODO: If we start supporting BYO key vaults, check whether the secret is enabled and valid or not.
                return secretBundle.Value;
            });
        }

        /// <summary>
        /// Create a new secret or update the value for an existing one.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="value">The secret value.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task CreateOrUpdateSecretAsync(string secretName, string value, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_create_or_update_secret", async childLogger =>
            {
                childLogger
                    .FluentAddValue("KeyVaultName", KeyVaultName)
                    .FluentAddValue("SecretName", secretName);

                Requires.NotNullOrEmpty(secretName, nameof(secretName));
                Requires.NotNullOrEmpty(value, nameof(value));

                var keyVaultClient = GetKeyVaultClient();
                await keyVaultClient.SetSecretAsync(KeyvaultBaseUrl, secretName, value);
            });
        }

        /// <summary>
        /// Delete a secret by secret name.
        /// </summary>
        /// <param name="secretName">The secret name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The secret value.</returns>
        public async Task DeleteSecretAsync(string secretName, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync($"{LoggingBaseName}_delete_secret", async childLogger =>
            {
                childLogger
                    .FluentAddValue("KeyVaultName", KeyVaultName)
                    .FluentAddValue("SecretName", secretName);

                Requires.NotNullOrEmpty(secretName, nameof(secretName));

                var keyVaultClient = GetKeyVaultClient();
                await keyVaultClient.DeleteSecretAsync(KeyvaultBaseUrl, secretName);
            });
        }

        private IKeyVaultClient GetKeyVaultClient()
        {
            if (cachedKeyVaultClient == null)
            {
                cachedKeyVaultClient = new KeyVaultClient(
                    async (authority, resource, scope) =>
                    {
                        var context = new AuthenticationContext(authority);
                        var clientSecret = await ServicePrincipal.GetClientSecretAsync();
                        var credential = new ClientCredential(ServicePrincipal.ClientId, clientSecret);
                        var authenticationResult = await context.AcquireTokenAsync(resource, credential);
                        return authenticationResult.AccessToken;
                    });
            }

            return cachedKeyVaultClient;
        }
    }
}
