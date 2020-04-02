// <copyright file="IControlPlaneAzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Factory to create Azure clients for the control plane subscription.
    /// </summary>
    public interface IControlPlaneAzureClientFactory
    {
        /// <summary>
        /// Get IAzure client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IAzure> GetAzureClientAsync();

        /// <summary>
        /// Get IComputeManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IComputeManagementClient> GetComputeManagementClient();

        /// <summary>
        /// Get INetworkManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<INetworkManagementClient> GetNetworkManagementClient();

        /// <summary>
        /// Get IResourceManagementClient client.
        /// </summary>
        /// <param name="subscriptionId">Azure subscription id for resource.</param>
        /// <returns>Azure client.</returns>
        Task<IResourceManagementClient> GetResourceManagementClient();
    }
}
