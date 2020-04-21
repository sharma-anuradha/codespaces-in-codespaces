// <copyright file="AzureClientFactoryMock.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// A mock <see cref="IAzureClientFactory"/>.
    /// </summary>
    public class AzureClientFactoryMock : IAzureClientFactory
    {
        private readonly string authFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactoryMock"/> class.
        /// </summary>
        /// <param name="authFile">The auth file.</param>
        public AzureClientFactoryMock(string authFile)
        {
            this.authFile = authFile;
        }

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(authFile);

            return await Task.FromResult(Microsoft.Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscriptionId.ToString()));
        }

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            return await GetAzureClientAsync(Guid.Parse(subscriptionId));
        }

        /// <inheritdoc/>
        public async Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
               .FromFile(authFile);
            var azureClient = new ComputeManagementClient(RestClient.Configure()
               .WithEnvironment(credentials.Environment)
               .WithCredentials(credentials)
               .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(credentials))
               .Build())
            { SubscriptionId = subscriptionId.ToString() };
            return await Task.FromResult<IComputeManagementClient>(azureClient);
        }

        /// <inheritdoc/>
        public Task<IStorageManagementClient> GetStorageManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<IKeyVaultManagementClient> GetKeyVaultManagementClient(Guid subscriptionId)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
               .FromFile(authFile);
            var azureClient = new NetworkManagementClient(RestClient.Configure()
               .WithEnvironment(credentials.Environment)
               .WithCredentials(credentials)
               .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(credentials))
               .Build())
            { SubscriptionId = subscriptionId.ToString() };
            return await Task.FromResult<INetworkManagementClient>(azureClient);
        }

        /// <inheritdoc/>
        public async Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
               .FromFile(authFile);
            var azureClient = new ResourceManagementClient(RestClient.Configure()
               .WithEnvironment(credentials.Environment)
               .WithCredentials(credentials)
               .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(credentials))
               .Build())
            { SubscriptionId = subscriptionId.ToString() };
            return await Task.FromResult<IResourceManagementClient>(azureClient);
        }
    }
}
