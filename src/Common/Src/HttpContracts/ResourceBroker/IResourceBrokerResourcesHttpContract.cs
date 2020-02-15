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
        Task<ResourceBrokerResource> GetAsync(Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Allocate a set of resources.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="allocateRequestBody">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<IEnumerable<AllocateResponseBody>> AllocateAsync(
            Guid environmentId, IEnumerable<AllocateRequestBody> allocateRequestBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a resource from the resource broker.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<bool> DeleteAsync(Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform suspend operations on a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="suspendRequestBody">Target resources to suspend.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been suspended.</returns>
        Task<bool> SuspendAsync(
            Guid environmentId, IEnumerable<SuspendRequestBody> suspendRequestBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Start the compute VM instance with the specified storage.
        /// </summary>
        /// <param name="computeResourceId">The compute resource id.</param>
        /// <param name="startComputeRequestBody">The bind input parameters.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task<bool> StartAsync(Guid computeResourceId, StartResourceRequestBody startComputeRequestBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates resource environment keepalive if exists.
        /// </summary>
        /// <param name="id">The target resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Whether the resource has not been deleted
        /// or in the process of being deleted.</returns>
        Task<bool> ProcessHeartbeatAsync(Guid id, IDiagnosticsLogger logger);
    }
}
