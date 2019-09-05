// <copyright file="AzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
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

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var azureSub = systemCatalog
                    .AzureSubscriptionCatalog
                    .AzureSubscriptions
                    .Single(sub => sub.SubscriptionId == azureSubscriptionId && sub.Enabled);

                IServicePrincipal sp = azureSub.ServicePrincipal;
                var azureAppId = sp.ClientId;
                var azureAppKey = await sp.GetServicePrincipalClientSecret();
                var azureTenant = sp.TenantId;

                return await GetAzureClientAsync(azureSubscriptionId, azureAppId, azureAppKey, azureTenant);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(azureSubscriptionId, ex);
            }
        }

        public async Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId)
        {
            var creds = new AzureCredentialsFactory()
                    .FromServicePrincipal(
                        azureAppId,
                        azureAppKey,
                        azureTenantId,
                        AzureEnvironment.AzureGlobalCloud);

            return Microsoft.Azure.Management.Fluent.Azure.Authenticate(creds)
                .WithSubscription(subscriptionId);
        }

        /// <inheritdoc/>
        public async Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var azureSub = systemCatalog
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

        /// <inheritdoc/>
        public async Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId)
        {
            var azureSubscriptionId = subscriptionId.ToString();
            try
            {
                var azureSub = systemCatalog
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
                var azureClient = new NetworkManagementClient(RestClient.Configure()
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
