// <copyright file="ICachedSystemConfiguration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Cached System configuration collection.
    /// </summary>
    public interface ICachedSystemConfiguration : ISystemConfiguration
    {
        /// <summary>
        /// Refresh the keys present in the cache.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RefreshCacheAsync(IDiagnosticsLogger logger);
    }
}