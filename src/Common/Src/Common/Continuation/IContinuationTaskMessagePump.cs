// <copyright file="IContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Message pump which gates messages to/from the underlying queue.
    /// </summary>
    public interface IContinuationTaskMessagePump : IDisposable
    {
        /// <summary>
        /// Tries to populate the local cahce from the queue.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether the task should continue.</returns>
        Task<bool> RunTryPopulateCacheAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets message from the cache if available or from the queue if needed.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>Found message.</returns>
        Task<CloudQueueMessage> GetMessageAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes message.
        /// </summary>
        /// <param name="message">Message to be deleted.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DeleteMessageAsync(CloudQueueMessage message, IDiagnosticsLogger logger);

        /// <summary>
        /// Pushes message onto queue.
        /// </summary>
        /// <param name="payload">Payload to be pushed.</param>
        /// <param name="initialVisibilityDelay">Initial visibility delay.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task PushMessageAsync(ContinuationQueuePayload payload, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);
    }
}
