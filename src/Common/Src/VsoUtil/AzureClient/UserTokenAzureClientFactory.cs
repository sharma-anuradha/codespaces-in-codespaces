// <copyright file="UserTokenAzureClientFactory.cs" company="Microsoft">
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
using Microsoft.Extensions.Options;
using Microsoft.Rest;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.VsoUtil.AzureClient
{
    /// <summary>
    /// Helper class for authenticating with Azure using user credentials (in the form of a JWT).
    /// </summary>
    public class UserTokenAzureClientFactory : IAzureClientFactory
    {
        private const string TenantId = "72f988bf-86f1-41af-91ab-2d7cd011db47";

        private readonly RestClient rootClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTokenAzureClientFactory"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public UserTokenAzureClientFactory(IOptions<UserTokenAzureClientFactoryOptions> options)
        {
            var tokenCredentials = new TokenCredentials(options.Value.AccessToken);

            var azureCredentials = new AzureCredentials(
                tokenCredentials,
                tokenCredentials,
                TenantId,
                AzureEnvironment.AzureGlobalCloud);

            rootClient = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(azureCredentials)
                .Build();
        }

        /// <inheritdoc/>
        public Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            return Task.FromResult(Microsoft.Azure.Management.Fluent.Azure
                .Authenticate(rootClient, TenantId)
                .WithSubscription(subscriptionId.ToString()));
        }

        /// <inheritdoc/>
        public Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            return Task.FromResult<IComputeManagementClient>(new ComputeManagementClient(rootClient)
            {
                SubscriptionId = subscriptionId.ToString(),
            });
        }

        /// <inheritdoc/>
        public Task<IKeyVaultManagementClient> GetKeyVaultManagementClient(Guid subscriptionId)
        {
            return Task.FromResult<IKeyVaultManagementClient>(new KeyVaultManagementClient(rootClient)
            {
                SubscriptionId = subscriptionId.ToString(),
            });
        }

        /// <inheritdoc/>
        public Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId)
        {
            return Task.FromResult<INetworkManagementClient>(new NetworkManagementClient(rootClient)
            {
                SubscriptionId = subscriptionId.ToString(),
            });
        }

        /// <inheritdoc/>
        public Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId)
        {
            return Task.FromResult<IResourceManagementClient>(new ResourceManagementClient(rootClient)
            {
                SubscriptionId = subscriptionId.ToString(),
            });
        }

        /// <inheritdoc/>
        public Task<IStorageManagementClient> GetStorageManagementClient(Guid subscriptionId)
        {
            return Task.FromResult<IStorageManagementClient>(new StorageManagementClient(rootClient)
            {
                SubscriptionId = subscriptionId.ToString(),
            });
        }
    }
}
