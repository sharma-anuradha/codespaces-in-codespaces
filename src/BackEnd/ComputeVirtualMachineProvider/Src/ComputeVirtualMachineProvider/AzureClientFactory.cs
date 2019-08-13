// <copyright file="AzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachine
{
    /// <summary>
    /// Build Azure Client.
    /// </summary>
    public class AzureClientFactory : IAzureClientFactory
    {
        private readonly ISystemCatalog systemCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureClientFactory"/> class.
        /// </summary>
        /// <param name="systemCatalog">provides service principle name.</param>
        public AzureClientFactory(ISystemCatalog systemCatalog)
        {
            this.systemCatalog = systemCatalog;
        }


        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var azureSub = this.systemCatalog
                    .AzureSubscriptionCatalog
                    .AzureSubscriptions
                    .Single(sub => sub.SubscriptionId == azureSubscriptionId && sub.Enabled);

                IServicePrincipal sp = azureSub.ServicePrincipal;
                string azureAppId = sp.ClientId;
                string azureAppKey = await sp.GetServicePrincipalClientSecret();
                string azureTenant = sp.TenantId;
                var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenant,
                        AzureEnvironment.AzureGlobalCloud);

                return Azure.Management.Fluent.Azure.Authenticate(creds)
                    .WithSubscription(azureSubscriptionId);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        public async Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var azureSub = this.systemCatalog
                    .AzureSubscriptionCatalog
                    .AzureSubscriptions
                    .Single(sub => sub.SubscriptionId == azureSubscriptionId && sub.Enabled);

                IServicePrincipal sp = azureSub.ServicePrincipal;
                string azureAppId = sp.ClientId;
                string azureAppKey = await sp.GetServicePrincipalClientSecret();
                string azureTenant = sp.TenantId;
                var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenant,
                        AzureEnvironment.AzureGlobalCloud);
                var azureClient = new ComputeManagementClient(RestClient.Configure()
                    .WithEnvironment(creds.Environment)
                    .WithCredentials(creds)
                    .WithDelegatingHandler(new ProviderRegistrationDelegatingHandler(creds))
                    .Build())
                { SubscriptionId = azureSub.SubscriptionId };
                return azureClient;
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }
    }
}
