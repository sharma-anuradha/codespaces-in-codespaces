// <copyright file="AzureClientFactoryMock.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    public class AzureClientFactoryMock : IAzureClientFactory
    {
        private readonly string authFile;

        public AzureClientFactoryMock(string authFile)
        {
            this.authFile = authFile;
        }

        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromFile(authFile);

            return await Task.FromResult(Microsoft.Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscriptionId.ToString()));
        }

        public async Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            return await GetAzureClientAsync(Guid.Parse(subscriptionId));
        }

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

        //public async Task<TClient> GetManagementClient<TClient>(Guid subscriptionId)
        //{
        //    var credentials = SdkContext.AzureCredentialsFactory
        //       .FromFile(authFile);
        //    RestClient restClient = RestClient.Configure()
        //       .WithEnvironment(credentials.Environment)
        //       .WithCredentials(credentials)
        //       .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(credentials))
        //       .Build();
        //    var constructor = typeof(TClient).GetConstructor(new[] { restClient.GetType() });
        //    TClient client = (TClient)constructor.Invoke(new[] { restClient });
        //    return await Task.FromResult<TClient>(client);
        //}
    }
}
