// <copyright file="IResourceBrokerResourcesHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The resource broker resource http contract.
    /// </summary>
    public interface IResourceBrokerResourcesHttpContract
    {
        /// <summary>
        /// Get a resource by id from the resource broker.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<ResourceBrokerResource> GetAsync(
            Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Allocate a set of resources.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="resources">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="loggingProperties">The dictionary of logging properties.</param>
        /// <returns>The allocation result.</returns>
        Task<IEnumerable<AllocateResponseBody>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateRequestBody> resources, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties = null);

        /// <summary>
        /// Delete a resource from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="action">The type of start request.</param>
        /// <param name="resources">The resources.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<bool> StartAsync(
            Guid environmentId, StartRequestAction action, IEnumerable<StartRequestBody> resources, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform suspend operations on a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="resources">Target resources to suspend.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been suspended.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a resource from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resources">The resource ids.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<bool> DeleteAsync(
            Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger);

        /// <summary>
        /// Get the status for a set of resources from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resources">The resource ids.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<IEnumerable<StatusResponseBody>> StatusAsync(
            Guid environmentId, IEnumerable<Guid> resources, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates resource environment keepalive if exists.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resourceId">The target resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Whether the resource has not been deleted
        /// or in the process of being deleted.</returns>
        Task<bool> ProcessHeartbeatAsync(
            Guid environmentId, Guid resourceId, IDiagnosticsLogger logger);
    }
}
