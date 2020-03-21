// <copyright file="IResourceBrokerResourcesExtendedHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
{
    /// <summary>
    /// The resource broker resource extended http contract.
    /// </summary>
    public interface IResourceBrokerResourcesExtendedHttpContract : IResourceBrokerResourcesHttpContract
    {
        /// <summary>
        /// Allocate a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="resource">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<AllocateResponseBody> AllocateAsync(
            Guid environmentId, AllocateRequestBody resource, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a resource from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="action">The type of start request.</param>
        /// <param name="resource">The resource.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<bool> StartAsync(Guid environmentId, StartRequestAction action, StartRequestBody resource, IDiagnosticsLogger logger);

        /// <summary>
        /// Perform suspend operations on a resource.
        /// </summary>
        /// <param name="environmentId">Environment id associated with the resource.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been cleaned.</returns>
        Task<bool> SuspendAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a resource from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<bool> DeleteAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Get the status for a resource from the resource broker.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="resourceId">The resource id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the resource has been deleted.</returns>
        Task<StatusResponseBody> StatusAsync(Guid environmentId, Guid resourceId, IDiagnosticsLogger logger);
    }
}
