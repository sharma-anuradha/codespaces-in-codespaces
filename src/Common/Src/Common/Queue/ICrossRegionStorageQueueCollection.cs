// <copyright file="ICrossRegionStorageQueueCollection.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Core queue collection interface for cross regions message passing.
    /// </summary>
    public interface ICrossRegionStorageQueueCollection
    {
        /// <summary>
        /// Adds content to the queue.
        /// </summary>
        /// <param name="content">Content that is being added.</param>
        /// <param name="controlPlaneRegion">Control plane region to which the message needs to be sent.</param>
        /// <param name="initialVisibilityDelay">Adds initial visibilit delay if needed.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        Task AddAsync(string content, AzureLocation controlPlaneRegion, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger);
    }
}
