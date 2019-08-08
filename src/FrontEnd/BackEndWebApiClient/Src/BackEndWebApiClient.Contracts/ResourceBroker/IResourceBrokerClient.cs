// <copyright file="IResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// A resource broker client.
    /// </summary>
    public interface IResourceBrokerClient
    {
        /// <summary>
        /// Allocate a resource.
        /// </summary>
        /// <param name="input">The allocation input properties.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The allocation result.</returns>
        Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Deallocate a resource.
        /// </summary>
        /// <param name="resourceIdToken">The resource id token.</param>
        /// <returns>True if the resource has been deallocated.</returns>
        Task<bool> DeallocateAsync(string resourceIdToken);

        /// <summary>
        /// Bind the compute VM instance with the specified storage.
        /// </summary>
        /// <param name="input">The bind input parameters..</param>
        /// <returns>The bind result.</returns>
        Task<BindResult> BindComputeToStorage(BindInput input);
    }
}
