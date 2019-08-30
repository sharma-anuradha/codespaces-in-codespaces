// <copyright file="IContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskMessagePump : IDisposable
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<bool> TryPopulateCacheAsync(IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task<IResourceJobQueueMessage> GetMessageAsync(IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task DeleteMessage(IResourceJobQueueMessage message, IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task AddPayloadAsync(ResourceJobQueuePayload payload, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);
    }
}
