// <copyright file="IStorageQueueClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.Azure.Storage.Queue;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// An Azure Storage Queue client provider.
    /// </summary>
    public interface IStorageQueueClientProvider
    {
        /// <summary>
        /// Gets the queue client for storage queue operations.
        /// </summary>
        CloudQueueClient QueueClient { get; }

        /// <summary>
        /// Gets a reference to a specific queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to get.</param>
        /// <returns>The cloud queue object that can be used to operate on the queue.</returns>
        Task<CloudQueue> GetQueueAsync([ValidatedNotNull] string queueName);
    }
}