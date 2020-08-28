// <copyright file="IConfigurationKeyGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator
{
    /// <summary>
    /// Marker interface for the Configuration key generator. For details please visit this wiki page - https://github.com/microsoft/vssaas-planning/wiki/Configuration-Key-Generator
    /// </summary>
    public interface IConfigurationKeyGenerator
    {
        /// <summary>
        /// Generates a service/global scoped configuration key.
        /// </summary>
        /// <param name="configurationType">Type of configuration the key is intended for.</param>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <returns>A <see cref="string"/> representing a generated key.</returns>
        string GenerateServiceScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName);

        /// <summary>
        /// Generates a region scoped configuration key.
        /// </summary>
        /// <param name="configurationType">Type of configuration the key is intended for.</param>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <returns>A <see cref="string"/> representing a generated key.</returns>
        string GenerateRegionScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName);

        /// <summary>
        /// Generates a subscription scoped configuration key.
        /// </summary>
        /// <param name="configurationType">Type of configuration the key is intended for.</param>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <param name="subscriptionId">Target subscription id.</param>
        /// <returns>A <see cref="string"/> representing a generated key.</returns>
        string GenerateSubscriptionScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, string subscriptionId);

        /// <summary>
        /// Generates a plan scoped configuration key.
        /// </summary>
        /// <param name="configurationType">Type of configuration the key is intended for.</param>
        /// <param name="componentName">Name of the component to which this key would be applicable for.</param>
        /// <param name="configurationName">Name of the configuration.</param>
        /// <param name="subscriptionId">Target subscription id.</param>
        /// <param name="planName">Target plan name.</param>
        /// <returns>A <see cref="string"/> representing a generated key.</returns>
        string GeneratePlanScopeConfigurationKey(ConfigurationType configurationType, string componentName, string configurationName, string subscriptionId, string planName);
    }
}
