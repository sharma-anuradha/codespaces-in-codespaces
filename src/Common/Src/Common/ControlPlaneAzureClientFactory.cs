// <copyright file="ControlPlaneAzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Build Azure Client.
    /// </summary>
    public class ControlPlaneAzureClientFactory : AzureClientFactoryBase, IControlPlaneAzureClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControlPlaneAzureClientFactory"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">The control plane resource accessor.</param>
        /// <param name="servicePrincipal">The app service principal.</param>
        public ControlPlaneAzureClientFactory(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IServicePrincipal servicePrincipal)
        {
            Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));

            ControlPlaneSubscriptionFunc = controlPlaneAzureResourceAccessor.GetCurrentSubscriptionIdAsync();
            ServicePrincipal = Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
        }

        private IServicePrincipal ServicePrincipal { get; }

        private Task<string> ControlPlaneSubscriptionFunc { get; }

        private Guid? ControlPlaneSubscriptionId { get; set; }

        /// <inheritdoc/>
        public async Task<IAzure> GetAzureClientAsync()
        {
            var subscriptionId = await GetSubscriptionIdAsync();
            try
            {
                return await GetAzureClientAsync(subscriptionId, ServicePrincipal);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IComputeManagementClient> GetComputeManagementClient()
        {
            var subscriptionId = await GetSubscriptionIdAsync();
            try
            {
                return await GetComputeManagementClient(subscriptionId, ServicePrincipal);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<INetworkManagementClient> GetNetworkManagementClient()
        {
            var subscriptionId = await GetSubscriptionIdAsync();
            try
            {
                return await GetNetworkManagementClient(subscriptionId, ServicePrincipal);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId, ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IResourceManagementClient> GetResourceManagementClient()
        {
            var subscriptionId = await GetSubscriptionIdAsync();
            try
            {
                return await GetResourceManagementClient(subscriptionId, ServicePrincipal);
            }
            catch (InvalidOperationException ex)
            {
                throw new AzureClientException(subscriptionId, ex);
            }
        }

        private async Task<Guid> GetSubscriptionIdAsync()
        {
            if (ControlPlaneSubscriptionId == null)
            {
                ControlPlaneSubscriptionId = Guid.Parse(await ControlPlaneSubscriptionFunc);
            }

            return ControlPlaneSubscriptionId.GetValueOrDefault();
        }
    }
}
