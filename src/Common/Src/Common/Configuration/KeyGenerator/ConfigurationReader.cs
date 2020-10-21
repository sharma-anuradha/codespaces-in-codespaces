// <copyright file="ConfigurationReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.Repository.Models;
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
        /// <param name="configurationScopeGenerator">Configuration scope generator.</param>
        /// <param name="cachedSystemConfigurationRepository">Target system configuration repository.</param>
        public ConfigurationReader(IConfigurationScopeGenerator configurationScopeGenerator, ICachedSystemConfigurationRepository cachedSystemConfigurationRepository)
        {
            ConfigurationScopeGenerator = configurationScopeGenerator;
            CachedSystemConfigurationRepository = cachedSystemConfigurationRepository;
        }

        private string LogBaseName => "configuration_reader";

        /// <summary>
        /// Gets the configuration scope generator.
        /// </summary>
        private IConfigurationScopeGenerator ConfigurationScopeGenerator { get; }

        /// <summary>
        /// Gets the system configuration repository.
        /// </summary>
        private ICachedSystemConfigurationRepository CachedSystemConfigurationRepository { get; }

        /// <inheritdoc/>
        public async Task<T> ReadFeatureFlagAsync<T>(string featureName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default)
        {
            return await ReadConfigurationByScope(context, ConfigurationType.Feature, featureName, ConfigurationConstants.EnabledFeatureName, defaultValue, logger);
        }

        /// <inheritdoc/>
        public async Task<T> ReadQuotaAsync<T>(string componentName, string quotaName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default)
        {
            return await ReadConfigurationByScope(context, ConfigurationType.Quota, componentName, quotaName, defaultValue, logger);
        }

        /// <inheritdoc/>
        public async Task<T> ReadSettingAsync<T>(string componentName, string settingName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default)
        {
            return await ReadConfigurationByScope(context, ConfigurationType.Setting, componentName, settingName, defaultValue, logger);
        }

        private async Task<T> ReadConfigurationByScope<T>(ConfigurationContext context, ConfigurationType configurationType, string componentName, string configurationName, T defaultValue, IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childlogger) =>
                {
                    var scopes = ConfigurationScopeGenerator.GetScopes(context);

                    foreach (var scope in scopes)
                    {
                        var key = ConfigurationHelpers.GetCompleteKey(scope, configurationType, componentName, configurationName);
                        var record = await CachedSystemConfigurationRepository.GetAsync(key, childlogger.NewChildLogger());

                        if (!string.IsNullOrEmpty(record?.Value))
                        {
                            var valueToReturn = ConvertType<T>(record);

                            childlogger.FluentAddValue("KeyUsedForValue", key)
                                .FluentAddValue("DefaultUsed", false)
                                .FluentAddValue("ScopeUsed", scope)
                                .FluentAddValue("IsOverrideSameAsDefault", EqualityComparer<T>.Default.Equals(valueToReturn, defaultValue));

                            return valueToReturn;
                        }
                    }

                    // Return default value if we couldn't find anything
                    childlogger.FluentAddValue("DefaultUsed", true);
                    return defaultValue;
                },
                (ex, childLogger) =>
                {
                    // return the default value in case of any exception
                    return Task.FromResult(defaultValue);
                }, swallowException: true);
        }

        private T ConvertType<T>(SystemConfigurationRecord record)
        {
            // Handling Nullable types (int?, double?, bool?, etc)
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
            {
                var conv = TypeDescriptor.GetConverter(typeof(T));
                return (T)conv.ConvertFrom(record.Value);
            }

            return (T)Convert.ChangeType(record.Value, typeof(T));
        }
    }
}