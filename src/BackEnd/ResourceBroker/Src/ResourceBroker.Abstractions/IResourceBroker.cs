// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions
{
    /// <summary>
    /// The resource broker that manages allocation, deallocation, and other high-level resource operations.
    /// </summary>
    public interface IResourceBroker
    {
        /// <summary>
        /// Allocate a compute or storage resource.
        /// </summary>
        /// <param name="input">The allocate input object.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>An <see cref="AllocateResult"/> object.</returns>
        Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Deallocate a resource.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resoruce was deleted.</returns>
        Task<bool> DeallocateAsync(string resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="continuationToken">Token used to continue the task.</param>
        /// <returns>A task.</returns>
        Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger);
    }
}