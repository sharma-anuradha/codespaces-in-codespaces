// <copyright file="IResourceJobQueueRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

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
        Task AddAsync(string id, IDiagnosticsLogger logger);
    }
}
