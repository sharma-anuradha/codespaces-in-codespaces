// <copyright file="ICrossRegionStorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An Azure Storage Queue client provider for cross region messaging.
    /// </summary>
    public interface ICrossRegionStorageQueueClientProvider
    {
        /// <summary>
        /// Gets the queue client for storage queue operations on the given region.
        /// </summary>
        /// <param name="controlPlaneRegion">Control plane region.</param>
        /// <returns>The cloud queue client instance.</returns>
        Task<CloudQueueClient> GetQueueClientAsync(
            AzureLocation controlPlaneRegion);

        /// <summary>
        /// Gets a reference to a specific queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to get.</param>
        /// /// <param name="controlPlaneRegion">Control plane region.</param>
        /// <returns>The cloud queue object that can be used to operate on the queue for the given region.</returns>
        Task<CloudQueue> GetQueueAsync(
            [ValidatedNotNull] string queueName,
            AzureLocation controlPlaneRegion);
    }
}