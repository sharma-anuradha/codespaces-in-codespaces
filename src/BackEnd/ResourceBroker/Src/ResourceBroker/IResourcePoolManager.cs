// <copyright file="IResourcePool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Manages the underlying resource pools.
    /// </summary>
    public interface IResourcePoolManager
    {
        /// <summary>
        /// Tries to obtain resource record from a pool witht the target attributes.
        /// </summary>
        /// <param name="poolCode">Target pool code.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Found resource record.</returns>
        Task<ResourceRecord> TryGetAsync(string poolCode, IDiagnosticsLogger logger);
    }
}
