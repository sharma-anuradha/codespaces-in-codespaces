// <copyright file="SystemConfigurationExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// System Configuration Extensions.
    /// </summary>
    public static class SystemConfigurationExtensions
    {
        /// <summary>
        /// Gets the current value for a given key subscription.
        /// </summary>
        /// <typeparam name="T">Type that the value should be cast to.</typeparam>
        /// <param name="systemConfiguration">Target system configuration.</param>
        /// <param name="key">Key that is being looked up.</param>
        /// <param name="subscriptionId">Target subscription id.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="defaultValue">Default value that should be used if key/value isn't found.</param>
        /// <returns>Current configuration value.</returns>
        public static Task<T> GetSubscriptionValueAsync<T>(
            this ISystemConfiguration systemConfiguration,
            string key,
            string subscriptionId,
            IDiagnosticsLogger logger,
            T defaultValue = default)
        {
            return systemConfiguration.GetValueAsync<T>($"{key}:{subscriptionId}", logger, defaultValue);
        }
    }
}
