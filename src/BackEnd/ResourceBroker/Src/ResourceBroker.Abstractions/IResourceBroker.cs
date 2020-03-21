// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// Allocate resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="inputs">Target input manifest.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An <see cref="AllocateResult"/> enumerable object.</returns>
        Task<IEnumerable<AllocateResult>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateInput> inputs, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Allocate a compute or storage resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The allocate input object.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>An <see cref="AllocateResult"/> object.</returns>
        Task<AllocateResult> AllocateAsync(
            Guid environmentId, AllocateInput input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="action">The start action.</param>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task<bool> StartAsync(
            Guid environmentId, StartAction action, IEnumerable<StartInput> input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="action">The start action.</param>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task<bool> StartAsync(
            Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Suspends resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendInput> input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Performs cleanup operations to be done before delete on the resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="inputs">The delete input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource was deleted.</returns>
        Task<bool> DeleteAsync(
           Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The deallocate input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource was deleted.</returns>
        Task<bool> DeleteAsync(
            Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Status check set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<IEnumerable<StatusResult>> StatusAsync(
            Guid environmentId, IEnumerable<StatusInput> input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Status check on a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<StatusResult> StatusAsync(
            Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks to see if a given resource exists.
        /// </summary>
        /// <param name="id">The target resource id.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource exists.</returns>
        Task<bool> ProcessHeartbeatAsync(
            Guid id, string trigger, IDiagnosticsLogger logger);
    }
}