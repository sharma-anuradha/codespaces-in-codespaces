// <copyright file="IEnvironmentContinuationOperations.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Resource continuation operations to make involving/starting specific handlers
    /// easier.
    /// </summary>
    public interface IEnvironmentContinuationOperations
    {
        /// <summary>
        /// Archive the environment by invoking the continution activator.
        /// </summary>
        /// <param name="environmentId">Target resource id.</param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="reason">Trigger for operation.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Resuling continuation result.</returns>
        Task<ContinuationResult> ArchiveAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create environment.
        /// </summary>
        /// <param name="environmentId">target env id.</param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="cloudEnvironmentOptions">env input.</param>
        /// <param name="startCloudEnvironmentParameters">env parameters.</param>
        /// <param name="reason">reason.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> CreateAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentOptions cloudEnvironmentOptions,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Resume environment.
        /// </summary>
        /// <param name="environmentId">target env id.</param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="startCloudEnvironmentParameters">env parameters.</param>
        /// <param name="reason">reason.</param>
        /// <param name="logger">logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> ResumeAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            StartCloudEnvironmentParameters startCloudEnvironmentParameters,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Shutdown the environment.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="forceSuspend">True to force suspend.</param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> ShutdownAsync(
            Guid environmentId,
            bool forceSuspend,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Updates System.
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="cloudEnvironmentParams">Env parameters.</param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> UpdateSystemAsync(
            Guid environmentId,
            DateTime lastStateUpdated,
            CloudEnvironmentParameters cloudEnvironmentParams,
            string reason,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Export the environment.
        /// </summary>
        /// <param name="environmentId">Target environment id. </param>
        /// <param name="lastStateUpdated">Target last state updated.</param>
        /// <param name="exportCloudEnvironmentParameters">Env parameters for exporting. </param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<ContinuationResult> ExportAsync(
          Guid environmentId,
          DateTime lastStateUpdated,
          ExportCloudEnvironmentParameters exportCloudEnvironmentParameters,
          string reason,
          IDiagnosticsLogger logger);

        /// <summary>
        /// Creates codespace for codespace hot pool.
        /// </summary>
        /// <param name="skuName">codespace sku name.</param>
        /// <param name="location">codespace pool location.</param>
        /// <param name="reason">Reason.</param>
        /// <param name="logger">Logger.</param>
        /// <returns></returns>
        Task CreatePoolResourceAsync(
            string skuName,
            AzureLocation location,
            string reason,
            IDiagnosticsLogger logger);
    }
}
