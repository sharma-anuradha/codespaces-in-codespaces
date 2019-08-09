// <copyright file="MockClientWorkspaceRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace
{
    /// <inheritdoc/>
    public class MockClientWorkspaceRepository : IWorkspaceRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockClientWorkspaceRepository"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The http client provider.</param>
        public MockClientWorkspaceRepository()
        {
        }

        /// <inheritdoc/>
        public async Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace)
        {
            await Task.CompletedTask;
            return new WorkspaceResponse
            {
                Id = Guid.NewGuid().ToString(),
                SessionToken = Guid.NewGuid().ToString(),
                HeartbeatInterval = TimeSpan.FromSeconds(60),
                Name = workspace.Name,
                ConnectionMode = workspace.ConnectionMode,
                AreAnonymousGuestsAllowed = workspace.AreAnonymousGuestsAllowed,
                ExpiresAt = workspace.ExpiresAt,
            };
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string workspaceId)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<WorkspaceResponse> GetStatusAsync(string workspaceId)
        {
            await Task.CompletedTask;
            return new WorkspaceResponse
            {
                Id = workspaceId,
                SessionToken = Guid.NewGuid().ToString(),
                HeartbeatInterval = TimeSpan.FromSeconds(60),
                Name = "test-connection",
                ConnectionMode = ConnectionMode.Auto,
                AreAnonymousGuestsAllowed = false,
                ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(1),
            };
        }
    }
}
