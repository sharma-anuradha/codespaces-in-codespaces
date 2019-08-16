// <copyright file="ICloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The front-end environment manager.
    /// </summary>
    public interface ICloudEnvironmentManager
    {
        /// <summary>
        /// Get an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="ownerId">The owning user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> GetEnvironmentAsync(string environmentId, string ownerId, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments owned by the given user id.
        /// </summary>
        /// <param name="ownerId">The owner's user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> GetEnvironmentsByOwnerAsync(string ownerId, IDiagnosticsLogger logger);

        /// <summary>
        /// Creates a new environment.
        /// </summary>
        /// <param name="environmentRegistration">The environment registration data.</param>
        /// <param name="options">The environment registration options.</param>
        /// <param name="callbackUriFormat">The callback uri format, to construct the callback with the new environment id.</param>
        /// <param name="ownerId">The ower id.</param>
        /// <param name="accessToken">The owner's access token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the new <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> CreateEnvironmentAsync(CloudEnvironment environmentRegistration, CloudEnvironmentOptions options, string callbackUriFormat, string ownerId, string accessToken, IDiagnosticsLogger logger);

        /// <summary>
        /// Update the callback information for an existing environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="options">The new callback options.</param>
        /// <param name="ownerId">The owner's user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateEnvironmentCallbackAsync(string environmentId, EnvironmentRegistrationCallbackOptions options, string ownerId, IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="ownerId">The owner's user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> DeleteEnvironmentAsync(string environmentId, string ownerId, IDiagnosticsLogger logger);
    }
}
