// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        /// <param name="input">The deallocate input.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource was deleted.</returns>
        Task<DeallocateResult> DeallocateAsync(DeallocateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Performs cleanup operations to be done before delete on the resource.
        /// </summary>
        /// <param name="input">The cleanup input.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the cleanup operation was successful.</returns>
        Task<CleanupResult> CleanupAsync(CleanupInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task<EnvironmentStartResult> StartComputeAsync(
            EnvironmentStartInput input,
            IDiagnosticsLogger logger);
    }
}