// <copyright file="IResourceRequestQueueProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    ///  Provides and creates resource request queues.
    /// </summary>
    public interface IResourceRequestQueueProvider
    {
        /// <summary>
        /// Delete pool queue.
        /// </summary>
        /// <param name="input">input to delete queue.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> DeletePoolQueueAsync(QueueProviderDeleteInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Get pool queue.
        /// </summary>
        /// <param name="poolCode">resource pool code.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<CloudQueue> GetPoolQueueAsync(string poolCode);

        /// <summary>
        /// Create / Update pool queues.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task UpdatePoolQueuesAsync(IDiagnosticsLogger logger);
    }
}