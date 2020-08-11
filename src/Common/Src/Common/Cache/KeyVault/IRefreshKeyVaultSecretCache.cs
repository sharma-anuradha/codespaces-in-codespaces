// <copyright file="IRefreshKeyVaultSecretCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Caches and refreshes secrets from the keyvault.
    /// </summary>
    public interface IRefreshKeyVaultSecretCache
    {
        /// <summary>
        /// Refresh the secrets and certificates present in the cache.
        /// </summary>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RefreshAllSecretsAsync(IDiagnosticsLogger logger);
    }
}