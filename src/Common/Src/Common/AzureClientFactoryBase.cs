// <copyright file="AzureClientFactoryBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Build Azure Clients.
    /// </summary>
    public abstract class AzureClientFactoryBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactoryBase"/> class.
        /// </summary>
        public AzureClientFactoryBase()
        {
        }

        /// <summary>
        /// Get an azure client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<IAzure> GetAzureClientAsync(Guid subscriptionId, IServicePrincipal sp)
        {
            try
            {
                var secret = await sp.GetClientSecretAsync();
                var creds = new AzureCredentialsFactory()
                        .FromServicePrincipal(
                            sp.ClientId,
                            secret,
                            sp.TenantId,
                            AzureEnvironment.AzureGlobalCloud);

                var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds)
                    .WithSubscription(subscriptionId.ToString());

                return azure;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId.ToString(), ex);
            }
        }

        /// <summary>
        /// Get a compute management client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId, IServicePrincipal sp)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateRestClientAsync(sp);
                var azureClient = new ComputeManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <summary>
        /// Get a network management client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId, IServicePrincipal sp)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateRestClientAsync(sp);
                var azureClient = new NetworkManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <summary>
        /// Get a storage management client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<IStorageManagementClient> GetStorageManagementClient(Guid subscriptionId, IServicePrincipal sp)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateRestClientAsync(sp);
                var azureClient = new StorageManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <summary>
        /// Get a key vault management client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<IKeyVaultManagementClient> GetKeyVaultManagementClient(Guid subscriptionId, IServicePrincipal sp)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateRestClientAsync(sp);
                var azureClient = new KeyVaultManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <summary>
        /// Get a resource management client.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="sp">The service principal.</param>
        /// <returns>The client.</returns>
        protected async Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId, IServicePrincipal sp)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var restClient = await CreateRestClientAsync(sp);
                var azureClient = new ResourceManagementClient(restClient)
                {
                    SubscriptionId = azureSubscriptionId,
                };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        private async Task<RestClient> CreateRestClientAsync(IServicePrincipal sp)
        {
            var azureAppId = sp.ClientId;
            var azureAppKey = await sp.GetClientSecretAsync();
            var azureTenant = sp.TenantId;
            var creds = new AzureCredentialsFactory()
                .FromServicePrincipal(
                    azureAppId,
                    azureAppKey,
                    azureTenant,
                    AzureEnvironment.AzureGlobalCloud);

            var restClient = RestClient.Configure()
                .WithEnvironment(creds.Environment)
                .WithCredentials(creds)
                .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(creds))
                .Build();

            return restClient;
        }
    }
}
