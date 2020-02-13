// <copyright file="AzureKeyVaultHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.VsCloudKernel.SignalService
{
    /// <summary>
    /// Azure key vault related helpers
    /// </summary>
    public static class AzureKeyVaultHelpers
    {
        /// <summary>
        /// Return multiple values of secrets on the key vault
        /// </summary>
        /// <param name="applicationServicePrincipal">An app service principal</param>
        /// <param name="keyVaultName">Name of the key vault to target</param>
        /// <param name="secretNameFilter">Callback to filter some of the secrets of key vault</param>
        /// <returns></returns>
        public static async Task<KeyValuePair<string, string>[]> GetSecretItemsAsync(
            this ApplicationServicePrincipal applicationServicePrincipal,
            string keyVaultName,
            Func<string, bool> secretNameFilter)
        {
            var keyVaultClient = CreateKeyVaultClient(applicationServicePrincipal);
            var vaultBaseUrl = $"https://{keyVaultName}.vault.azure.net";
            var pageSecretItems = await keyVaultClient.GetSecretsAsync(vaultBaseUrl);

            var secretItems = new List<KeyValuePair<string, string>>();

            while (true)
            {
                foreach (var secretItem in pageSecretItems.Where(s => secretNameFilter(s.Identifier.Name)))
                {
                    var secretBundle = await keyVaultClient.GetSecretAsync(vaultBaseUrl, secretItem.Identifier.Name);
                    secretItems.Add(new KeyValuePair<string, string>(secretItem.Identifier.Name, secretBundle.Value));
                }

                if (string.IsNullOrEmpty(pageSecretItems.NextPageLink))
                {
                    break;
                }

                pageSecretItems = await keyVaultClient.GetSecretsNextAsync(pageSecretItems.NextPageLink);
            }

            return secretItems.ToArray();
        }

        private static IKeyVaultClient CreateKeyVaultClient(ApplicationServicePrincipal applicationServicePrincipal)
        {
            return new KeyVaultClient(
                async (authority, resource, scope) =>
                {
                    var context = new AuthenticationContext(authority);
                    var credential = new ClientCredential(applicationServicePrincipal.ClientId, applicationServicePrincipal.ClientPassword);
                    var authenticationResult = await context.AcquireTokenAsync(resource, credential);
                    return authenticationResult.AccessToken;
                });
        }
    }
}
