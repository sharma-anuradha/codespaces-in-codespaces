// <copyright file="IAzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Factory to create Azure clients.
    /// </summary>
    public interface IAzureClientFactory
    {
        /// <summary>
        /// Get IAzure client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IAzure> GetAzureClientAsync(Guid subscriptionId);

        /// <summary>
        /// Gets IAzure client, given relavant info.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for the resource.</param>
        /// <param name="azureAppId">Azure app id.</param>
        /// <param name="azureAppKey">Azure app key.</param>
        /// <param name="azureTenantId">Azure tenant id.</param>
        /// <returns>Azure client.</returns>
        Task<IAzure> GetAzureClientAsync(string subscriptionId, string azureAppId, string azureAppKey, string azureTenantId);

        /// <summary>
        /// Get IComputeManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IComputeManagementClient> GetComputeManagementClient(Guid subscriptionId);

        /// <summary>
        /// Get INetworkManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<INetworkManagementClient> GetNetworkManagementClient(Guid subscriptionId);

        /// <summary>
        /// Get IResourceManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IResourceManagementClient> GetResourceManagementClient(Guid subscriptionId);
    }
}
