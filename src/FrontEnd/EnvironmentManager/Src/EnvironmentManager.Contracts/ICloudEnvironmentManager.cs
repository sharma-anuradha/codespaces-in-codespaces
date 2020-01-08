// <copyright file="ICloudEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

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
        /// <param name="ownerIdSet">The owning user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> GetEnvironmentAsync(string environmentId, UserIdSet ownerIdSet, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments owned by the given user id.
        /// </summary>
        /// <param name="ownerIdSet">The owner's user id set. Required unless plan ID is specified.</param>
        /// <param name="name">Optional environment name to filter on.</param>
        /// <param name="planId">Optional plan ID to filter on.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> ListEnvironmentsAsync(
            UserIdSet ownerIdSet, string name, string planId, IDiagnosticsLogger logger);

        /// <summary>
        /// Creates a new environment.
        /// </summary>
        /// <param name="environmentRegistration">The environment registration data.</param>
        /// <param name="options">The environment registration options.</param>
        /// <param name="serviceUri">The service uri, to construct let cloudenv extension connect to the right service from server.</param>
        /// <param name="callbackUriFormat">The callback uri format, to construct the callback with the new environment id.</param>
        /// <param name="ownerIdSet">The owner id.</param>
        /// <param name="accessToken">The owner's access token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> CreateEnvironmentAsync(
            CloudEnvironment environmentRegistration,
            CloudEnvironmentOptions options,
            Uri serviceUri,
            string callbackUriFormat,
            UserIdSet ownerIdSet,
            string accessToken,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update the callback information for an existing environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="options">The new callback options.</param>
        /// <param name="ownerIdSet">The owner's user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateEnvironmentCallbackAsync(
            string environmentId,
            EnvironmentRegistrationCallbackOptions options,
            UserIdSet ownerIdSet,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Deletes an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="ownerIdSet">The owner's user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the environment was deleted, otherwise false.</returns>
        Task<bool> DeleteEnvironmentAsync(string environmentId, UserIdSet ownerIdSet, IDiagnosticsLogger logger);

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="serviceUri">The service uri, to construct let cloudenv extension connect to the right service from server.</param>
        /// <param name="callbackUriFormat">The callback uri format, to construct the callback with the new environment id.</param>
        /// <param name="currentUserIdSet">The current user id.</param>
        /// <param name="accessToken">The owner's access token.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> StartEnvironmentAsync(string id, Uri serviceUri, string callbackUriFormat, UserIdSet currentUserIdSet, string accessToken, IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down an environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="currentUserIdSet">The current user id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="internalTrigger">Sepcifies whether the shutdown was triggered internally, in which case, <paramref name="currentUserIdSet"/> must be null.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> ShutdownEnvironmentAsync(string id, UserIdSet currentUserIdSet, IDiagnosticsLogger logger, bool internalTrigger = false);

        /// <summary>
        /// Update Environment.
        /// </summary>
        /// <param name="cloudEnvironment">Cloud Environemnt that needs to be updated.</param>
        /// <param name="newState">New state, if the state needs to be updated.</param>
        /// <param name="trigger">The trigger for the state change.</param>
        /// <param name="reason">Reason for state change, if the state needs to be updated.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateEnvironmentAsync(CloudEnvironment cloudEnvironment, CloudEnvironmentState newState, string trigger, string reason, IDiagnosticsLogger logger);

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

        /// <summary>
        /// Updates the given environment's settings according to the given update request.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="update">The update request.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="CloudEnvironmentAvailableSettingsUpdates"/> providing the allowed settings updates.</returns>
        Task<CloudEnvironmentSettingsUpdateResult> UpdateEnvironmentSettingsAsync(
            string id,
            CloudEnvironmentUpdate update,
            ICurrentUserProvider currentUserProvider,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get the environment settings which are allowed on the given environment.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="currentUser">The current user.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="CloudEnvironmentAvailableSettingsUpdates"/> providing the allowed settings updates.</returns>
        CloudEnvironmentAvailableSettingsUpdates GetEnvironmentAvailableSettingsUpdates(
            CloudEnvironment cloudEnvironment,
            Profile currentUser,
            IDiagnosticsLogger logger);
    }
}
