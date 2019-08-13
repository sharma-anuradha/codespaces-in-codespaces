// <copyright file="AzureClientFactoryMock.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
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

            return await Task.FromResult(Azure.Management.Fluent.Azure.Authenticate(credentials).WithSubscription(subscriptionId.ToString()));
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

    }
}
