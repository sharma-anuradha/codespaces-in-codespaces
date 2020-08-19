// <copyright file="IQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Define a queue contract.
    /// </summary>
    public interface IQueue
    {
        /// <summary>
        /// Gets the queue id for this instance.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Add a message to this queue.
        /// </summary>
        /// <param name="content">Content of the message to be added.</param>
        /// <param name="initialVisibilityDelay">Initial visibility timeout delay.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task with the message instance.</returns>
        Task<QueueMessage> AddMessageAsync(byte[] content, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieve queued messages from this queue.
        /// </summary>
        /// <param name="messageCount">Total count of messages to retrieve.</param>
        /// <param name="visibilityTimeout">Optional visibility timeout after removing from this queue.</param>
        /// <param name="timeout">Timeout to wait for messages.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task with a retrieved messages.</returns>
        Task<IEnumerable<QueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, TimeSpan timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Delete a message from this queue that was previuously retrieved.
        /// </summary>
        /// <param name="queueMessage">The message queue instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task DeleteMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken);

        /// <summary>
        /// Update an existing message.
        /// </summary>
        /// <param name="queueMessage">The message queue instance.</param>
        /// <param name="updateContent">If update the queue nessage content.</param>
        /// <param name="visibilityTimeout">The update visibility timeout to be refreshed.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task UpdateMessageAsync(QueueMessage queueMessage, bool updateContent, TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    }
}
