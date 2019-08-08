// <copyright file="IWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;

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
        /// <returns>A workspace response.</returns>
        Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace);

        /// <summary>
        /// Delete a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <returns>A task.</returns>
        Task DeleteAsync(string workspaceId);

        /// <summary>
        /// Get a workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace id.</param>
        /// <returns>The workspace.</returns>
        Task<WorkspaceResponse> GetStatusAsync(string workspaceId);
    }
}
