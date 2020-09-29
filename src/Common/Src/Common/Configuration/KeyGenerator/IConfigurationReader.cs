// <copyright file="IConfigurationReader.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    public interface IConfigurationReader
    {
        /// <summary>
        /// Gets the current value for a given setting key.
        /// </summary>
        /// <typeparam name="T">Type that the value should be cast to.</typeparam>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="settingName">Name of the setting.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="defaultValue">Default value that should be used if key/value isn't found.</param>
        /// <param name="context">Context describing applicable scopes for the configuration</param>
        /// <returns>Current configuration value.</returns>
        Task<T> ReadSettingAsync<T>(string componentName, string settingName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default);

        /// <summary>
        /// Gets the current value for a given feature flag key.
        /// </summary>
        /// <typeparam name="T">Type that the value should be cast to.</typeparam>
        /// <param name="featureName">Name of the feature.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="defaultValue">Default value that should be used if key/value isn't found.</param>
        /// <param name="context">Context describing applicable scopes for the configuration</param>
        /// <returns>Current configuration value.</returns>
        Task<T> ReadFeatureFlagAsync<T>(string featureName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default);

        /// <summary>
        /// Gets the current value for a given quota key.
        /// </summary>
        /// <typeparam name="T">Type that the value should be cast to.</typeparam>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="quotaName">Name of the setting.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="defaultValue">Default value that should be used if key/value isn't found.</param>
        /// <param name="context">Context describing applicable scopes for the configuration</param>
        /// <returns>Current configuration value.</returns>
        Task<T> ReadQuotaAsync<T>(string componentName, string quotaName, IDiagnosticsLogger logger, T defaultValue = default, ConfigurationContext context = default);
    }
}
