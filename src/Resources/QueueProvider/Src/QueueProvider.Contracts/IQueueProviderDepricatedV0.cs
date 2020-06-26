// <copyright file="IQueueProviderDepricatedV0.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts
{
    /// <summary>
    /// Queue provided depricated interface.
    /// This won't work with sharded storage accounts.
    /// Note: can be removed after all environments are suspended/resumed.
    /// </summary>
    public interface IQueueProviderDepricatedV0
    {
        /// <summary>
        /// Deletes the queue.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DeleteAsync(
            AzureLocation location,
            string queueName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Check if queue exists.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<object> ExistsAync(
            AzureLocation location,
            string queueName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Pushes a message into the queue.
        /// </summary>
        /// <param name="location">Azure location.</param>
        /// <param name="queueName">Queue name.</param>
        /// <param name="queueMessage">Queue message.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task PushMessageAsync(
            AzureLocation location,
            string queueName,
            QueueMessage queueMessage,
            IDiagnosticsLogger logger);
    }
}
