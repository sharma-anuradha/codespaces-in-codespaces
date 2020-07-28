// <copyright file="IResourceBroker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// The resource broker that manages allocation, deallocation, and other high-level resource operations.
    /// </summary>
    public interface IResourceBroker : IAllocateResource
    {
        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="action">The start action.</param>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>A task.</returns>
        Task<bool> StartAsync(
            Guid environmentId, StartAction action, IEnumerable<StartInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Start compute with storage and startup parameters.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="action">The start action.</param>
        /// <param name="input">Input for the environment to be started.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>A task.</returns>
        Task<bool> StartAsync(
            Guid environmentId, StartAction action, StartInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Suspends resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Performs cleanup operations to be done before delete on the resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, SuspendInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete resource set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="inputs">The delete input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the resource was deleted.</returns>
        Task<bool> DeleteAsync(
           Guid environmentId, IEnumerable<DeleteInput> inputs, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The deallocate input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the resource was deleted.</returns>
        Task<bool> DeleteAsync(
            Guid environmentId, DeleteInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Status check set based on input manifest.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<IEnumerable<StatusResult>> StatusAsync(
            Guid environmentId, IEnumerable<StatusInput> input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Status check on a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="input">The suspend input.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the suspend operation was successful.</returns>
        Task<StatusResult> StatusAsync(
            Guid environmentId, StatusInput input, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Checks to see if a given resource exists.
        /// </summary>
        /// <param name="id">The target resource id.</param>
        /// <param name="trigger">Target trgger.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>True if the resource exists.</returns>
        Task<bool> ProcessHeartbeatAsync(
            Guid id, string trigger, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);
    }
}