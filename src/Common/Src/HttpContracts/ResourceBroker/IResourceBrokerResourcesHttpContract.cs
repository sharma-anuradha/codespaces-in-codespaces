// <copyright file="IResourceBrokerResourcesHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        /// <param name="allocateRequestBody">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<ResourceBrokerResource> CreateResourceAsync(CreateResourceRequestBody allocateRequestBody, IDiagnosticsLogger logger);

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
        /// Start the compute VM instance with the specified storage.
        /// </summary>
        /// <param name="computeResourceId">The compute resource id.</param>
        /// <param name="startComputeRequestBody">The bind input parameters.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task StartComputeAsync(Guid computeResourceId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger);
    }
}
