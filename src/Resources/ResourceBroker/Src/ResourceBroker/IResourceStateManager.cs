// <copyright file="IResourceStateManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Updates Resource state.
    /// </summary>
    public interface IResourceStateManager
    {
        /// <summary>
        /// Queue up resource request.
        /// </summary>
        /// <param name="resource">Resource.</param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ResourceRecord> MarkResourceReady(ResourceRecord resource, string reason, IDiagnosticsLogger logger);
    }
}