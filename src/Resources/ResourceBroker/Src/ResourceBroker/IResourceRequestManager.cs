// <copyright file="IResourceRequestManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Fulfills resource requests on first come first serve basis.
    /// </summary>
    public interface IResourceRequestManager
    {
        /// <summary>
        /// Queue up resource request.
        /// </summary>
        /// <param name="resourcePool">Resource pool.</param>
        /// <param name="loggingProperties">Logging properties.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ResourceRecord> EnqueueAsync(ResourcePool resourcePool, IDictionary<string, string> loggingProperties, IDiagnosticsLogger logger);

        /// <summary>
        /// Try assign resource to queued requests.
        /// </summary>
        /// <param name="resource">Resource record.</param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task<ResourceRecord> TryAssignAsync(ResourceRecord resource, string reason, IDiagnosticsLogger logger);
    }
}