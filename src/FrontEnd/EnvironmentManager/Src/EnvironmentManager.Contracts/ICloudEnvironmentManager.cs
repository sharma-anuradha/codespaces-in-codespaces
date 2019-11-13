// <copyright file="ICloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

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
        /// <param name="ownerId">The owner's user id. Required unless plan ID is specified.</param>
        /// <param name="name">Optional environment name to filter on.</param>
        /// <param name="planId">Optional plan ID to filter on.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> ListEnvironmentsAsync(
            string ownerId, string name, string planId, IDiagnosticsLogger logger);

        /// <summary>
        /// Creates a new environment.
        /// </summary>
        /// <param name="environmentRegistration">The environment registration data.</param>
        /// <param name="options">The environment registration options.</param>
        /// <param name="serviceUri">The service uri, to construct let cloudenv extension connect to the right service from server.</param>
        /// <param name="callbackUriFormat">The callback uri format, to construct the callback with the new environment id.</param>
        /// <param name="ownerId">The owner id.</param>
        /// <param name="ownerProviderId">The provider id of the owner profile.</param>
        /// <param name="accessToken">The owner's access token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> CreateEnvironmentAsync(
            CloudEnvironment environmentRegistration,
            CloudEnvironmentOptions options,
            Uri serviceUri,
            string callbackUriFormat,
            string ownerId,
            string ownerProviderId,
            string accessToken,
            IDiagnosticsLogger logger);

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

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="serviceUri">The service uri, to construct let cloudenv extension connect to the right service from server.</param>
        /// <param name="callbackUriFormat">The callback uri format, to construct the callback with the new environment id.</param>
        /// <param name="currentUserId">The current user id.</param>
        /// <param name="accessToken">The owner's access token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> StartEnvironmentAsync(string id, Uri serviceUri, string callbackUriFormat, string currentUserId, string accessToken, IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down an environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="currentUserId">The current user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> ShutdownEnvironmentAsync(string id, string currentUserId, IDiagnosticsLogger logger);

        /// <summary>
        /// Update Environment.
        /// </summary>
        /// <param name="cloudEnvironment">Cloud Environemnt that needs to be updated.</param>
        /// <param name="newState">New state, if the state needs to be updated.</param>
        /// <param name="reason">Reason for state change, if the state needs to be updated.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateEnvironmentAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string reason, IDiagnosticsLogger logger);

        /// <summary>
        /// Get environment by id.
        /// </summary>
        /// <param name="id">The environment by id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<CloudEnvironment> GetEnvironmentByIdAsync(string id, IDiagnosticsLogger logger);

        /// <summary>
        /// Callback to be executed after running shutdown on the vm.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>A task.</returns>
        Task ShutdownEnvironmentCallbackAsync(string id, IDiagnosticsLogger logger);

        /// <summary>
        /// Force suspends an environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A task.</returns>
        Task ForceEnvironmentShutdownAsync(string id, IDiagnosticsLogger logger);
    }
}
