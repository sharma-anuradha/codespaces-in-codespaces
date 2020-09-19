// <copyright file="ConfigurationReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// A key generator for various configurations. For details please visit this wiki page - https://github.com/microsoft/vssaas-planning/wiki/Configuration-Reader
    /// </summary>
    public class ConfigurationReader : IConfigurationReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationKeyGenerator"/> class.
        /// </summary>
        /// <param name="configurationKeyGenerator">Configuration key generator.</param>
        /// <param name="cachedSystemConfiguration">Target system configuration.</param>
        public ConfigurationReader(IConfigurationKeyGenerator configurationKeyGenerator, ICachedSystemConfiguration cachedSystemConfiguration)
        {
            ConfigurationKeyGenerator = configurationKeyGenerator;
            CachedSystemConfiguration = cachedSystemConfiguration;
        }

        /// <summary>
        /// Gets the configuration key generator.
        /// </summary>
        private IConfigurationKeyGenerator ConfigurationKeyGenerator { get; }

        /// <summary>
        /// Gets the system configuration cache object.
        /// </summary>
        private ICachedSystemConfiguration CachedSystemConfiguration { get; }

        /// <inheritdoc/>
        public async Task<T> ReadFeatureFlagAsync<T>(string componentName, IDiagnosticsLogger logger, T defaultValue = default)
        {
            var regionScopedKey = ConfigurationKeyGenerator.GenerateRegionScopeConfigurationKey(ConfigurationType.Feature, componentName, ConfigurationConstants.EnabledFeatureName);
            return await CachedSystemConfiguration.GetValueAsync(regionScopedKey, logger, defaultValue);
        }

        /// <inheritdoc/>
        public async Task<T> ReadSettingAsync<T>(string componentName, string settingName, IDiagnosticsLogger logger, T defaultValue = default)
        {
            var regionScopedKey = ConfigurationKeyGenerator.GenerateRegionScopeConfigurationKey(ConfigurationType.Setting, componentName, settingName);
            return await CachedSystemConfiguration.GetValueAsync(regionScopedKey, logger, defaultValue);
        }
    }
}