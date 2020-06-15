// <copyright file="IResourceSelectorFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Resource selector.
    /// </summary>
    public interface IResourceSelectorFactory
    {
        /// <summary>
        /// Creates list of allocation requests.
        /// </summary>
        /// <param name="cloudEnvironment">Cloud environment record.</param>
        /// <param name="cloudEnvironmentOptions">Cloud environment options.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>List of allocation requests.</returns>
        Task<IList<AllocateRequestBody>> CreateAllocationRequestsAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentOptions cloudEnvironmentOptions, IDiagnosticsLogger logger);
    }
}