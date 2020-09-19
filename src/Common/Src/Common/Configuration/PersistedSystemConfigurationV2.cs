// <copyright file="PersistedSystemConfigurationV2.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Configuration provider backed by repository.
    /// </summary>
    public class PersistedSystemConfigurationV2 : ICachedSystemConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedSystemConfigurationV2"/> class.
        /// </summary>
        /// <param name="configurationRepository">Target configuration repository.</param>
        public PersistedSystemConfigurationV2(
            ICachedSystemConfigurationRepository configurationRepository)
        {
            ConfigurationRepository = configurationRepository;
        }

        private ICachedSystemConfigurationRepository ConfigurationRepository { get; }

        /// <inheritdoc/>
        public async Task<T> GetValueAsync<T>(string key, IDiagnosticsLogger logger, T defaultValue = default)
        {
            var result = await ConfigurationRepository.GetAsync(key, logger.NewChildLogger());

            // Return default if no result was found
            if (string.IsNullOrEmpty(result?.Value))
            {
                return defaultValue;
            }

            // Handling Nullable types (int?, double?, bool?, etc)
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                var conv = TypeDescriptor.GetConverter(typeof(T));
                return (T)conv.ConvertFrom(result.Value);
            }

            return (T)Convert.ChangeType(result.Value, typeof(T));
        }

        /// <inheritdoc/>
        public async Task RefreshCacheAsync(IDiagnosticsLogger logger)
        {
            await ConfigurationRepository.RefreshCacheAsync(logger.NewChildLogger());    
        }
    }
}
