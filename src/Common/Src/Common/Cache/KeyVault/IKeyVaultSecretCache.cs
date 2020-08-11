// <copyright file="IKeyVaultSecretCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Caches and refreshes secrets from the keyvault.
    /// </summary>
    /// <typeparam name="T">Type of the secret.</typeparam>
    public interface IKeyVaultSecretCache<T>
    {
        /// <summary>
        /// Get the secret from the cache. If it is not present in the cache, fetches the secret and
        /// add it to the cache.
        /// </summary>
        /// <param name="key">key.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{T}"/> representing the result of the asynchronous operation.</returns>
        Task<T> GetSecretAsync(string key, IDiagnosticsLogger logger);
    }
}