// <copyright file="IWorkspaceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Manages live share workspaces.
    /// </summary>
    public interface IWorkspaceManager
    {
        /// <summary>
        /// Create workspace.
        /// </summary>
        /// <param name="environmentType">environment type.</param>
        /// <param name="environmentId">environment id.</param>
        /// <param name="computeResourceId">compute id.</param>
        /// <param name="connectionServiceUri">connection uri.</param>
        /// <param name="sessionPath">session path.</param>
        /// <param name="emailAddress">email address of the host.</param>
        /// <param name="authToken">auth token.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        Task<ConnectionInfo> CreateWorkspaceAsync(
            EnvironmentType environmentType,
            string environmentId,
            Guid computeResourceId,
            Uri connectionServiceUri,
            string sessionPath,
            string emailAddress,
            string authToken,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get workspace status.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>A task.</returns>
        Task<WorkspaceResponse> GetWorkspaceStatusAsync(
            string workspaceId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>The workspace.</returns>
        Task DeleteWorkspaceAsync(
            string workspaceId,
            IDiagnosticsLogger logger);
    }
}