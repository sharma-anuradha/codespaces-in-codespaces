// <copyright file="ControlPlaneAzureResourceAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// The azure secret provider.
    /// </summary>
    public class ControlPlaneAzureResourceAccessor : IControlPlaneAzureResourceAccessor, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneAzureResourceAccessor"/> class.
        /// </summary>
        /// <param name="options">The azure resource provider options.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="servicePrincipal">The application service principal.</param>
        /// <param name="httpClient">The http client singleton.</param>
        public ControlPlaneAzureResourceAccessor(
            IControlPlaneInfo controlPlaneInfo,
            IServicePrincipal servicePrincipal,
            HttpClientWrapper httpClient)
        {
            ServicePrincipal = Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            HttpClient = Requires.NotNull(httpClient, nameof(httpClient));
        }

        private IServicePrincipal ServicePrincipal { get; }
        
        private IControlPlaneInfo ControlPlaneInfo { get; }

        private HttpClientWrapper HttpClient { get; }

        private string CurrentSubscriptionId { get; set; }

        private (string, string) InstanceCosmosDbAccount { get; set; }

        private (string, string) StampCosmosDbAccount { get; set; }

        private (string, string) StampStorageAccount { get; set; }

        private AzureCredentials AzureCredentials { get; set; }

        private ICosmosDBManager CosmosDbManager { get; set; }

        private IStorageManagementClient StorageManagementClient { get; set; }

        /// <inheritdoc/>
        public async Task<string> GetCurrentSubscriptionIdAsync()
        {
            if (!string.IsNullOrEmpty(CurrentSubscriptionId))
            {
                return CurrentSubscriptionId;
            }

            // Try the settings first
            if (ControlPlaneInfo.TryGetSubscriptionId(out var subscriptionId))
            {
                CurrentSubscriptionId = subscriptionId;
                return CurrentSubscriptionId;
            }

            using (var response = await HttpClient.GetAsync(@"http://169.254.169.254/metadata/instance/compute/subscriptionId?api-version=2018-10-01&format=text"))
            {
                response.EnsureSuccessStatusCode();

                CurrentSubscriptionId = (await response.Content.ReadAsStringAsync())?.ToLower();
                return CurrentSubscriptionId;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetKeyVaultSecretAsync(string secretName, string version = null)
        {
            using (var keyVaultClient = await GetKeyVaultClientAsync())
            {
                var keyvaultBaseUrl = $"https://{ControlPlaneInfo.EnvironmentKeyVaultName}.vault.azure.net/";

                SecretBundle secretBundle = null;
                if (string.IsNullOrWhiteSpace(version))
                {
                    secretBundle = await keyVaultClient.GetSecretAsync(keyvaultBaseUrl, secretName);
                }
                else
                {
                    secretBundle = await keyVaultClient.GetSecretAsync(keyvaultBaseUrl, secretName, version);
                }

                return secretBundle.Value;
            }
        }

        /// <inheritdoc/>
        public string GetDNSHostName()
        {
            return this.ControlPlaneInfo.DnsHostName;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<SecretItem>> GetKeyVaultSecretVersionsAsync(string secretName)
        {
            using (var keyVaultClient = await GetKeyVaultClientAsync())
            {
                var keyvaultBaseUrl = $"https://{ControlPlaneInfo.EnvironmentKeyVaultName}.vault.azure.net/";
                var versions = await keyVaultClient.GetSecretVersionsAsync(keyvaultBaseUrl, secretName);
                return versions;
            }
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetInstanceCosmosDbAccountAsync()
        {
            if (string.IsNullOrEmpty(InstanceCosmosDbAccount.Item1) ||
                string.IsNullOrEmpty(InstanceCosmosDbAccount.Item2))
            {
                InstanceCosmosDbAccount = await GetCosmosDbAccountAsync(
                    ControlPlaneInfo.InstanceResourceGroupName,
                    ControlPlaneInfo.InstanceCosmosDbAccountName);
            }

            return InstanceCosmosDbAccount;
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampCosmosDbAccountAsync()
        {
            if (string.IsNullOrEmpty(StampCosmosDbAccount.Item1) ||
                string.IsNullOrEmpty(StampCosmosDbAccount.Item2))
            {
                StampCosmosDbAccount = await GetCosmosDbAccountAsync(
                    ControlPlaneInfo.Stamp.StampResourceGroupName,
                    ControlPlaneInfo.Stamp.StampCosmosDbAccountName);
            }

            return StampCosmosDbAccount;
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampStorageAccountAsync()
        {
            if (string.IsNullOrEmpty(StampStorageAccount.Item1) ||
                string.IsNullOrEmpty(StampStorageAccount.Item2))
            {
                StampStorageAccount = await GetStorageAccountAsync(
                    ControlPlaneInfo.Stamp.StampResourceGroupName,
                    ControlPlaneInfo.Stamp.StampStorageAccountName,
                    default);
            }

            return StampStorageAccount;
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampStorageAccountForComputeQueuesAsync(AzureLocation computeVmLocation, IDiagnosticsLogger logger)
        {
            var storageAccountName = ControlPlaneInfo.Stamp.GetStampStorageAccountNameForComputeQueues(computeVmLocation);
            return await GetStorageAccountAsync(
                ControlPlaneInfo.Stamp.StampResourceGroupName,
                storageAccountName,
                logger);
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampStorageAccountForComputeVmAgentImagesAsync(AzureLocation computeVmLocation)
        {
            var storageAccountName = ControlPlaneInfo.Stamp.GetStampStorageAccountNameForComputeVmAgentImages(computeVmLocation);
            return await GetStorageAccountAsync(
                ControlPlaneInfo.Stamp.StampResourceGroupName,
                storageAccountName,
                default);
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampStorageAccountForStorageImagesAsync(AzureLocation computeStorageLocation)
        {
            var storageAccountName = ControlPlaneInfo.Stamp.GetStampStorageAccountNameForStorageImages(computeStorageLocation);
            return await GetStorageAccountAsync(
                ControlPlaneInfo.Stamp.StampResourceGroupName,
                storageAccountName,
                default);
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            var storageManagementClient = StorageManagementClient;
            StorageManagementClient = null;
            storageManagementClient?.Dispose();
        }

        private async Task<IKeyVaultClient> GetKeyVaultClientAsync()
        {
            await Task.CompletedTask;

            return new KeyVaultClient(
                async (authority, resource, scope) =>
                {
                    var context = new AuthenticationContext(authority);
                    var clientSecret = await ServicePrincipal.GetServicePrincipalClientSecretAsync();
                    var credential = new ClientCredential(ServicePrincipal.ClientId, clientSecret);
                    var authenticationResult = await context.AcquireTokenAsync(resource, credential);
                    return authenticationResult.AccessToken;
                });
        }

        private async Task<(string, string)> GetCosmosDbAccountAsync(string resourceGroup, string accountName)
        {
            var cosmosDbManager = await GetCosmosDBManagerAsync();
            var cosmosDbAccount = await cosmosDbManager.CosmosDBAccounts.GetByResourceGroupAsync(resourceGroup, accountName);
            var endpoint = cosmosDbAccount.DocumentEndpoint;
            var keys = await cosmosDbAccount.ListKeysAsync();
            var key = keys.PrimaryMasterKey;
            return (endpoint, key);
        }

        private async Task<(string, string)> GetStorageAccountAsync(string resourceGroup, string accountName, IDiagnosticsLogger logger)
        {
            Requires.NotNull(resourceGroup, nameof(resourceGroup));
            Requires.NotNull(accountName, nameof(accountName));

            var storageManagementClient = await GetStorageManagementClientAsync();
            try
            {
                var keys = await storageManagementClient.StorageAccounts.ListKeysAsync(resourceGroup, accountName);
                return (accountName, keys.Keys.First().Value);
            }
            catch (Exception)
            {
                logger?.FluentAddValue(nameof(accountName), accountName)
                    .FluentAddValue(nameof(resourceGroup), resourceGroup)
                    .LogError("get_storage_account_error");
                throw;
            }
        }

        private async Task<ICosmosDBManager> GetCosmosDBManagerAsync()
        {
            if (CosmosDbManager != null)
            {
                return CosmosDbManager;
            }

            var subscriptionId = await GetCurrentSubscriptionIdAsync();
            var azureCredentials = await GetAzureCredentialsAsync();
            CosmosDbManager = new CosmosDBManager(ConfigureRestClient(azureCredentials), subscriptionId);
            return CosmosDbManager;
        }

        private async Task<IStorageManagementClient> GetStorageManagementClientAsync()
        {
            if (StorageManagementClient != null)
            {
                return StorageManagementClient;
            }

            var subscriptionId = await GetCurrentSubscriptionIdAsync();
            var azureCredentials = await GetAzureCredentialsAsync();
            StorageManagementClient = new StorageManagementClient(ConfigureRestClient(azureCredentials))
            {
                SubscriptionId = subscriptionId,
            };

            return StorageManagementClient;
        }

        private RestClient ConfigureRestClient(AzureCredentials creds)
        {
            return RestClient.Configure()
                .WithEnvironment(creds.Environment)
                .WithCredentials(creds)
                .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(creds))
                .Build();
        }

        private async Task<AzureCredentials> GetAzureCredentialsAsync()
        {
            if (AzureCredentials == null)
            {
                IServicePrincipal sp = ServicePrincipal;
                string azureAppId = sp.ClientId;
                string azureAppKey = await sp.GetServicePrincipalClientSecretAsync();
                string azureTenant = sp.TenantId;
                var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenant,
                        AzureEnvironment.AzureGlobalCloud);
                AzureCredentials = creds;
            }

            return AzureCredentials;
        }

        /// <summary>
        /// The custom http client.
        /// </summary>
        public class HttpClientWrapper
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
            /// </summary>
            /// <param name="httpClient">The injected http client instance.</param>
            public HttpClientWrapper(HttpClient httpClient)
            {
                HttpClient = httpClient;
                httpClient.DefaultRequestHeaders.Add("Metadata", "True");
            }

            private HttpClient HttpClient { get; }

            /// <summary>
            /// Invoke HTTP GET.
            /// </summary>
            /// <param name="requestUri">The request Uri.</param>
            /// <returns>The response message.</returns>
            public async Task<HttpResponseMessage> GetAsync(string requestUri)
            {
                return await HttpClient.GetAsync(requestUri);
            }
        }
    }
}
