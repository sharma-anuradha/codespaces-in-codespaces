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
        /// Allocate a resource from the resource broker.
        /// </summary>
        /// <param name="requestBodies">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<IEnumerable<ResourceBrokerResource>> CreateResourceSetAsync(
            IEnumerable<CreateResourceRequestBody> createResourcesRequestBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Get a resource by id from the resource broker.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<ResourceBrokerResource> GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Deallocate a resource from the resource broker.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deallocated.</returns>
        Task<bool> DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform cleanup operations prior to delete on a resource from the resource broker.
        /// </summary>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been cleaned.</returns>
        Task<bool> CleanupResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Start the compute VM instance with the specified storage.
        /// </summary>
        /// <param name="computeResourceId">The compute resource id.</param>
        /// <param name="startComputeRequestBody">The bind input parameters.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task StartComputeAsync(Guid computeResourceId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger);

        /// <summary>
        /// Checks to see if a given resource exists.
        /// </summary>
        /// <param name="id">The target resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource exists.</returns>
        Task<bool> TriggerEnvironmentHeartbeatAsync(Guid id, IDiagnosticsLogger logger);
    }
}
