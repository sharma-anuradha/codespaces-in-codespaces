// <copyright file="IResourceAllocationManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceAllocation
{
    /// <summary>
    /// Manages resource allocation.
    /// </summary>
    public interface IResourceAllocationManager
    {
        /// <summary>
        /// Allocate resources for environment.
        /// </summary>
        /// <param name="environmentId">environment id.</param>
        /// <param name="allocateRequests">resource list.</param>
        /// <param name="logger">logger.result.</param>
        /// <returns>result.</returns>
        Task<IEnumerable<ResourceAllocationRecord>> AllocateResourcesAsync(
            Guid environmentId,
            IEnumerable<AllocateRequestBody> allocateRequests,
            IDiagnosticsLogger logger);
    }
}