// <copyright file="BaseKeyVaultSecretCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.KeyVault;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Base key vault secret cache.
    /// </summary>
    /// <typeparam name="T">Type of the secret.</typeparam>
    public abstract class BaseKeyVaultSecretCache<T> : IKeyVaultSecretCache<T>, IRefreshKeyVaultSecretCache
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseKeyVaultSecretCache{T}"/> class.
        /// </summary>
        /// <param name="keyVaultSecretReader">keyVault reader.</param>
        /// <param name="controlPlaneInfo">control plane info.</param>
        public BaseKeyVaultSecretCache(
            IKeyVaultSecretReader keyVaultSecretReader,
            IControlPlaneInfo controlPlaneInfo)
        {
            Cache = new Dictionary<string, T>();
            KeyVaultSecretReader = Requires.NotNull(keyVaultSecretReader, nameof(keyVaultSecretReader));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        /// <summary>
        /// Gets the log base name.
        /// </summary>
        protected string LogBaseName => "key_vault_secret_cache";

        /// <summary>
        /// Gets the key vault secret reader.
        /// </summary>
        protected IKeyVaultSecretReader KeyVaultSecretReader { get; }

        /// <summary>
        /// Gets the cache object.
        /// </summary>
        protected IDictionary<string, T> Cache { get; }

        /// <summary>
        /// Gets the control plane info object.
        /// </summary>
        protected IControlPlaneInfo ControlPlaneInfo { get; }

        /// <inheritdoc/>
        public virtual async Task RefreshAllSecretsAsync(IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
               $"{LogBaseName}_refresh_all",
               async (childLogger) =>
               {
                   List<Task> tasks = new List<Task>();
                   foreach (var key in Cache.Keys)
                   {
                       tasks.Add(RefreshSecretAsync(key, childLogger.NewChildLogger()));
                   }

                   await Task.WhenAll(tasks);
               }, swallowException: true);
        }

        /// <inheritdoc/>
        public async Task<T> GetSecretAsync(string key, IDiagnosticsLogger logger)
        {
            if (Cache.TryGetValue(key, out var value))
            {
                return value;
            }
            else
            {
                var secret = await RefreshSecretAsync(key, logger);
                return secret;
            }
        }

        /// <summary>
        /// Get the secret from the cache. If it is not present in the cache, fetches the secret and
        /// add it to the cache.
        /// </summary>
        /// <param name="key">key.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{T}"/> representing the result of the asynchronous operation.</returns>
        protected abstract Task<T> RefreshSecretAsync(string key, IDiagnosticsLogger logger);
    }
}
