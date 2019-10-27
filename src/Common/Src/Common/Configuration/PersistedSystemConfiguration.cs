// <copyright file="PersistedSystemConfiguration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Configuration provider backed by repository.
    /// </summary>
    public class PersistedSystemConfiguration : ISystemConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistedSystemConfiguration"/> class.
        /// </summary>
        /// <param name="configurationRepository">Target configuration repository.</param>
        public PersistedSystemConfiguration(
            ISystemConfigurationRepository configurationRepository)
        {
            ConfigurationRepository = configurationRepository;
        }

        private ISystemConfigurationRepository ConfigurationRepository { get; }

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
    }
}
