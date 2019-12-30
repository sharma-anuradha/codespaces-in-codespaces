// <copyright file="IStorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// An Azure Storage Queue client provider.
    /// </summary>
    public interface IStorageQueueClientProvider
    {
        /// <summary>
        /// Gets the queue client for storage queue operations.
        /// </summary>
        /// <returns>The cloud queue client instance.</returns>
        Task<CloudQueueClient> GetQueueClientAsync();

        /// <summary>
        /// Gets a reference to a specific queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to get.</param>
        /// <returns>The cloud queue object that can be used to operate on the queue.</returns>
        Task<CloudQueue> GetQueueAsync([ValidatedNotNull] string queueName);
    }
}