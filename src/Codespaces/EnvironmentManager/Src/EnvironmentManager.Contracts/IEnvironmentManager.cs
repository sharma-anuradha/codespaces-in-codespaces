// <copyright file="IEnvironmentManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The front-end environment manager.
    /// </summary>
    public interface IEnvironmentManager
    {
        /// <summary>
        /// Get environment by id without any ownership validation.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<CloudEnvironment> GetAsync(
            Guid environmentId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments owned by the given user id.
        /// </summary>
        /// <param name="planId">Optional plan ResourceId to query for.</param>
        /// <param name="location">Optional plan location, if known.</param>
        /// <param name="name">Optional environment FriendlyName to query for (case-insensitive).</param>
        /// <param name="userIdSet">The owner's user id set. Required unless plan ID is specified.</param>
        /// <param name="environmentListType">Indicates whether list is active/deleted/all environments.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> ListAsync(
            string planId,
            AzureLocation? location,
            string name,
            UserIdSet userIdSet,
            EnvironmentListType environmentListType,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update Environment Status.
        /// </summary>
        /// <param name="cloudEnvironmentId">Cloud environment id.</param>
        /// <param name="newState">New state, if the state needs to be updated.</param>
        /// <param name="trigger">The trigger for the state change.</param>
        /// <param name="reason">Reason for state change, if the state needs to be updated.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateStatusAsync(
            Guid cloudEnvironmentId,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update Environment.
        /// </summary>
        /// <param name="cloudEnvironment">Cloud Environemnt that needs to be updated.</param>
        /// <param name="newState">New state, if the state needs to be updated.</param>
        /// <param name="trigger">The trigger for the state change.</param>
        /// <param name="reason">Reason for state change, if the state needs to be updated.</param>
        /// <param name="isUserError">Is failure because of user error.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentState newState,
            string trigger,
            string reason,
            bool? isUserError,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update the callback information for an existing environment.
        /// </summary>
        /// <param name="cloudEnvironment">The environment.</param>
        /// <param name="options">The new callback options.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the updated <see cref="CloudEnvironment"/>.</returns>
        Task<CloudEnvironment> UpdateCallbackAsync(
            CloudEnvironment cloudEnvironment,
            EnvironmentRegistrationCallbackOptions options,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Creates a new environment.
        /// </summary>
        /// <param name="details">Target details.</param>
        /// <param name="startEnvironmentParams">Target start environment params.</param>
        /// <param name="metricsInfo">Target metrics info.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironment> CreateAsync(
            EnvironmentCreateDetails details,
            StartCloudEnvironmentParameters startEnvironmentParams,
            MetricsInfo metricsInfo,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Soft Deletes an environment.
        /// </summary>
        /// <param name="cloudEnvironmentId">The environment Id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the environment was soft-deleted, otherwise false.</returns>
        Task<bool> SoftDeleteAsync(Guid cloudEnvironmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Hard Deletes an environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if the environment was hard-deleted, otherwise false.</returns>
        Task<bool> HardDeleteAsync(Guid environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Restores an environment.
        /// </summary>
        /// <param name="cloudEnvironmentId">The environment ID.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud Environment.</returns>
        Task<CloudEnvironment> DeleteRestoreAsync(
            Guid cloudEnvironmentId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="startCloudEnvironmentParameters">The parameters for staring compute.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The cloud environment record.</returns>
        Task<CloudEnvironment> ResumeAsync(
            Guid environmentId,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Exports environment.
        /// </summary>
        /// <param name="cloudEnvironment">The environment id.</param>
        /// <param name="exportCloudEnvironmentParameters">The parameters for starting compute.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironment> ExportAsync(
            Guid cloudEnvironment,
            ExportCloudEnvironmentParameters exportCloudEnvironmentParameters,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Completes the start of a shutdown environment.
        /// </summary>
        /// <param name="environmentId">Target environment Id.</param>
        /// <param name="storageResourceId">Target new storage that should be swapped in.</param>
        /// <param name="archiveStorageResourceId">Target archive storage resource id if waking from archive.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The cloud environment record.</returns>
        Task<CloudEnvironment> ResumeCallbackAsync(Guid environmentId, Guid storageResourceId, Guid? archiveStorageResourceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Completes the start of an archived environment.
        /// </summary>
        /// <param name="cloudEnvironment">The environment.</param>
        /// <param name="storageResourceId">Target new storage that should be swapped in.</param>
        /// <param name="archiveStorageResourceId">Target archive storage resource id if waking from archive.</param>
        /// <param name="exportedEnvironmentUrl">Exported environment url.</param>
        /// <param name="exportedBranch">The exported git branch.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironment> ExportCallbackAsync(Guid cloudEnvironment, Guid storageResourceId, Guid? archiveStorageResourceId, string exportedEnvironmentUrl, string exportedBranch, IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down an environment.
        /// </summary>
        /// <param name="environmentId">The environment Id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The cloud environment record.</returns>
        Task<CloudEnvironment> SuspendAsync(Guid environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Shuts down an environment.
        /// </summary>
        /// <param name="environmentId">The environment Id.</param>
        /// <param name="isClientSuspend">True if the client initiated the suspend.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The cloud environment record.</returns>
        Task<CloudEnvironment> SuspendAsync(Guid environmentId, bool isClientSuspend, IDiagnosticsLogger logger);

        /// <summary>
        /// Callback to be executed after running shutdown on the vm.
        /// </summary>
        /// <param name="cloudEnvironment">The environment.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>Cloud environment service result.</returns>
        Task<CloudEnvironmentServiceResult> SuspendCallbackAsync(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger);

        /// <summary>
        /// Force suspends an environment.
        /// </summary>
        /// <param name="environmentId">The environment Id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The cloud environment record.</returns>
        Task<CloudEnvironment> ForceSuspendAsync(Guid environmentId, IDiagnosticsLogger logger);

        /// <summary>
        /// Updates the given environment's settings according to the given update request.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="update">The update request.</param>
        /// or null if the update is not a move.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="CloudEnvironmentAvailableSettingsUpdates"/> providing the allowed settings updates.</returns>
        Task<CloudEnvironmentUpdateResult> UpdateSettingsAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentUpdate update,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get the environment settings which are allowed on the given environment.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="CloudEnvironmentAvailableSettingsUpdates"/> providing the allowed settings updates.</returns>
        Task<CloudEnvironmentAvailableSettingsUpdates> GetAvailableSettingsUpdatesAsync(
            CloudEnvironment cloudEnvironment,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Replaces the given environment's recent folders list with the given updated list.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment.</param>
        /// <param name="update">The updated list.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>A <see cref="CloudEnvironmentUpdateResult"/> the result of updating the environment. </returns>
        Task<CloudEnvironmentUpdateResult> UpdateFoldersListAsync(
            CloudEnvironment cloudEnvironment,
            CloudEnvironmentFolderBody update,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Start Compute.
        /// </summary>
        /// <param name="cloudEnvironment">cloud environment.</param>
        /// <param name="computeResourceId">compute resource id.</param>
        /// <param name="osDiskResourceId">os disk resource id.</param>
        /// <param name="storageResourceId">storage resource id.</param>
        /// <param name="archiveStorageResourceId">archive storage id.</param>
        /// <param name="cloudEnvironmentParameters">start environment params.</param>
        /// <param name="startEnvironmentAction">start environment action.</param>
        /// <param name="logger">logger.</param>
        /// <returns>resule.</returns>
        Task<bool> StartComputeAsync(
            CloudEnvironment cloudEnvironment,
            Guid computeResourceId,
            Guid? osDiskResourceId,
            Guid? storageResourceId,
            Guid? archiveStorageResourceId,
            CloudEnvironmentParameters cloudEnvironmentParameters,
            StartEnvironmentAction startEnvironmentAction,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets all environments under the given Azure subscription.
        /// </summary>
        /// <param name="subscription">Required Azure subscription.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A task whose result is the list of <see cref="CloudEnvironment"/>.</returns>
        Task<IEnumerable<CloudEnvironment>> ListBySubscriptionAsync(Subscription subscription, IDiagnosticsLogger logger);
    }
}
