// <copyright file="IWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <summary>
    /// A Live Share workspace repository.
    /// </summary>
    public interface IWorkspaceRepository
    {
        /// <summary>
        /// Create a workspace.
        /// </summary>
        /// <param name="workspace">The workspace request.</param>
        /// <param name="logger">Diagnostic logger</param>
        /// <returns>A workspace response.</returns>
        Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <param name="logger">Diagnostic logger</param>
        /// <returns>A task.</returns>
        Task DeleteAsync(string workspaceId, IDiagnosticsLogger logger);

        /// <summary>
        /// Get a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <param name="logger">Diagnostic logger</param>
        /// <returns>The workspace.</returns>
        Task<WorkspaceResponse> GetStatusAsync(string workspaceId, IDiagnosticsLogger logger);
    }
}
