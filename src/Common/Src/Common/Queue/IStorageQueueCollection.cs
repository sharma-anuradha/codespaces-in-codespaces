// <copyright file="IStorageQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Core queue collection interface.
    /// </summary>
    public interface IStorageQueueCollection
    {
        /// <summary>
        /// Adds content to the queue.
        /// </summary>
        /// <param name="content">Content that is being added.</param>
        /// <param name="timeToLive">The time to live for the content added.</param>
        /// <param name="initialVisibilityDelay">Adds initial visibilit delay if needed.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task AddAsync(string content, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);

        /// <summary>
        /// Pulls multiple messages from the queue.
        /// </summary>
        /// <param name="popCount">How many messages are requested.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="timeout">Target timeout.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task<IEnumerable<CloudQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger, TimeSpan? timeout = null);

        /// <summary>
        /// Pulls single messages from the queue.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <param name="timeout">Target timeout.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task<CloudQueueMessage> GetAsync(IDiagnosticsLogger logger, TimeSpan? timeout = null);

        /// <summary>
        /// Deletes message from the queue.
        /// </summary>
        /// <param name="message">Target message.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DeleteAsync(CloudQueueMessage message, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the aproximate message count sitting on the queue.
        /// </summary>
        /// <param name="logger">the logger.</param>
        /// <returns>the approximate number of items avaialble on the queue.</returns>
        Task<int?> GetApproximateMessageCount(IDiagnosticsLogger logger);
    }
}
