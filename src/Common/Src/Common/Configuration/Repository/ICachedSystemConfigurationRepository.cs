// <copyright file="ICachedSystemConfigurationRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository
{
    /// <summary>
    /// Cached Configuration Repository.
    /// </summary>
    public interface ICachedSystemConfigurationRepository : ISystemConfigurationRepository
    {
        /// <summary>
        /// Refresh the keys present in the cache.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RefreshCacheAsync(IDiagnosticsLogger logger);
    }
}
