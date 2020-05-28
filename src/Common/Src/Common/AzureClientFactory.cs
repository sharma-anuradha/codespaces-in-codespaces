// <copyright file="AzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Build Azure Client.
    /// </summary>
    public class AzureClientFactory : AzureClientFactoryBase, IAzureClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactory"/> class.
        /// </summary>
        /// <param name="subscriptionCatalog">The azure subscription catalog..</param>
        public AzureClientFactory(
            IAzureSubscriptionCatalog subscriptionCatalog)
        {
            SubscriptionCatalog = Requires.NotNull(subscriptionCatalog, nameof(subscriptionCatalog));
        }

        private IAzureSubscriptionCatalog SubscriptionCatalog { get; }

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetAzureClientAsync(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenantId,
                        AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds)
                .WithSubscription(subscriptionId);
            return Task.FromResult(azure);
        }

        /// <inheritdoc/>
        public async Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetComputeManagementClient(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetNetworkManagementClient(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IStorageManagementClient> GetStorageManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetStorageManagementClient(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IKeyVaultManagementClient> GetKeyVaultManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetKeyVaultManagementClient(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var sp = GetServicePrincipalForSubscription(subscriptionId);
                return await GetResourceManagementClient(subscriptionId, sp);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        private IServicePrincipal GetServicePrincipalForSubscription(Guid azureSubscriptionId)
        {
            var subscriptionId = azureSubscriptionId.ToString();
            var azureSub = this.SubscriptionCatalog
                    .AzureSubscriptionsIncludingInfrastructure()
                    .Single(sub => sub != default && sub.SubscriptionId == subscriptionId && sub.Enabled);
            return azureSub.ServicePrincipal;
        }
    }
}
