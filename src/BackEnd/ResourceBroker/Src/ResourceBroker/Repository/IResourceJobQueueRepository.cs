// <copyright file="IResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository
{
    /// <summary>
    ///
    /// </summary>
    public interface IResourceJobQueueRepository
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="id">Id of the item in the queue.</param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task AddAsync(string id, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="popCount"></param>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IEnumerable<IResourceJobQueueMessage>> GetAsync(int popCount, IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<IResourceJobQueueMessage> GetAsync(IDiagnosticsLogger logger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        Task DeleteAsync(IResourceJobQueueMessage message, IDiagnosticsLogger logger);
    }
}
