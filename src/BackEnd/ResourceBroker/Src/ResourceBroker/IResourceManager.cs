// <copyright file="IResourceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manager that coordinates resource orchistration efforts.
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// Adds resource request to job queue.
        /// </summary>
        /// <param name="skuName">Name of the targeted sku.</param>
        /// <param name="type">Type of resource being targeted.</param>
        /// <param name="location">Location of the targeted resource.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task gating completion gate.</returns>
        Task AddResourceCreationRequestToJobQueueAsync(
            string skuName,
            ResourceType type,
            string location,
            IDiagnosticsLogger logger);
    }
}
