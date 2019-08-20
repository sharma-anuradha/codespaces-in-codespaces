// <copyright file="IAzureClientFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Network.Fluent;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Abstractions
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
    }
}
