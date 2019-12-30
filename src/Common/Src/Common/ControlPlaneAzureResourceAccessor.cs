﻿// <copyright file="ControlPlaneAzureResourceAccessor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.Batch.Fluent;
using Microsoft.Azure.Management.CosmosDB.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VsSaaS.Caching;
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
        /// <param name="cache">The cache to use for caching various Azure keys to reduce load on ARM.</param>
        public ControlPlaneAzureResourceAccessor(
            IControlPlaneInfo controlPlaneInfo,
            IServicePrincipal servicePrincipal,
            HttpClientWrapper httpClient,
            IManagedCache cache)
        {
            ServicePrincipal = Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            HttpClient = Requires.NotNull(httpClient, nameof(httpClient));
            Cache = Requires.NotNull(cache, nameof(cache));
        }

        private IServicePrincipal ServicePrincipal { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private HttpClientWrapper HttpClient { get; }

        private IManagedCache Cache { get; }

        private string CurrentSubscriptionId { get; set; }

        private (string, string) InstanceCosmosDbAccount { get; set; }

        private (string, string) StampCosmosDbAccount { get; set; }

        private (string, string) StampStorageAccount { get; set; }

        private AzureCredentials AzureCredentials { get; set; }

        private ICosmosDBManager CosmosDbManager { get; set; }

        private IStorageManagementClient StorageManagementClient { get; set; }

        private IBatchManagementClient BatchManagementClient { get; set; }

        private TimeSpan CacheExpiration { get; } = TimeSpan.FromHours(1);

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
        public List<string> GetStampOrigins()
        {
            var allDNSNames = ControlPlaneInfo.AllStamps.Values.Select(s => $"https://{s.DnsHostName}")
                .Distinct()
                .ToList();

            allDNSNames.Add($"https://{ControlPlaneInfo.DnsHostName}");

            return allDNSNames;
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
        public async Task<(string, string, string)> GetStampBatchAccountAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            var resourceGroup = ControlPlaneInfo.Stamp.StampResourceGroupName;
            var accountName = ControlPlaneInfo.Stamp.GetStampBatchAccountName(location);

            var cacheKey = $"batchKey:{resourceGroup}:{accountName}";
            var cacheEndpointKey = $"batchEndpoint:{resourceGroup}:{accountName}";

            var key = await Cache.GetAsync<string>(cacheKey, logger);
            var endpoint = await Cache.GetAsync<string>(cacheEndpointKey, logger);
            if (key == null || endpoint == null)
            {
                try
                {
                    var batchManagementClient = await GetBatchManagementClientAsync();
                    var batchAccount = await batchManagementClient.BatchAccount.GetAsync(
                        resourceGroup,
                        accountName);
                    var accountKeys = await batchManagementClient.BatchAccount.GetKeysAsync(
                        resourceGroup,
                        accountName);

                    key = accountKeys.Primary;
                    endpoint = $"https://{batchAccount.AccountEndpoint}";
                }
                catch (Exception)
                {
                    logger.FluentAddValue(nameof(accountName), accountName)
                        .FluentAddValue(nameof(resourceGroup), resourceGroup)
                        .LogError("get_batch_account_error");
                    throw;
                }

                await Cache.SetAsync(cacheKey, key, CacheExpiration, logger);
                await Cache.SetAsync(cacheEndpointKey, endpoint, CacheExpiration, logger);
            }

            return (accountName, key, endpoint);
        }

        /// <inheritdoc/>
        public async Task<(string, string)> GetStampStorageAccountForBillingSubmission(AzureLocation billingSubmissionLocation)
        {
            var storageAccountName = ControlPlaneInfo.Stamp.GetStampStorageAccountNameForBillingSubmission(billingSubmissionLocation);
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

            var cacheKey = $"storageKey:{resourceGroup}:{accountName}";
            var key = await Cache.GetAsync<string>(cacheKey, logger);
            if (key == null)
            {
                var storageManagementClient = await GetStorageManagementClientAsync();
                try
                {
                    var keys = await storageManagementClient.StorageAccounts.ListKeysAsync(resourceGroup, accountName);
                    key = keys.Keys.First().Value;
                }
                catch (Exception)
                {
                    logger?.FluentAddValue(nameof(accountName), accountName)
                        .FluentAddValue(nameof(resourceGroup), resourceGroup)
                        .LogError("get_storage_account_error");
                    throw;
                }

                await Cache.SetAsync(cacheKey, key, CacheExpiration, logger);
            }

            return (accountName, key);
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

        private async Task<IBatchManagementClient> GetBatchManagementClientAsync()
        {
            if (BatchManagementClient != null)
            {
                return BatchManagementClient;
            }

            var subscriptionId = await GetCurrentSubscriptionIdAsync();
            var azureCredentials = await GetAzureCredentialsAsync();
            BatchManagementClient = new BatchManagementClient(ConfigureRestClient(azureCredentials))
            {
                SubscriptionId = subscriptionId,
            };

            return BatchManagementClient;
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
                var sp = ServicePrincipal;
                var azureAppId = sp.ClientId;
                var azureAppKey = await sp.GetServicePrincipalClientSecretAsync();
                var azureTenant = sp.TenantId;
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
