// <copyright file="ISystemConfiguration.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// System configuration.
    /// </summary>
    public interface ISystemConfiguration
    {
        /// <summary>
        /// Gets the current value for a given key.
        /// </summary>
        /// <typeparam name="T">Type that the value should be cast to.</typeparam>
        /// <param name="key">Key that is being looked up.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="defaultValue">Default value that should be used if key/value isn't found.</param>
        /// <returns>Current configuration value.</returns>
        Task<T> GetValueAsync<T>(string key, IDiagnosticsLogger logger, T defaultValue = default);
    }
}
