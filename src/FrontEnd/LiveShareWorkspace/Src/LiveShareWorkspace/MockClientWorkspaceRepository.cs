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

        /// <summary>Gets or sets a function for returning a mocked Create response.</summary>
        public Func<WorkspaceRequest, WorkspaceResponse> MockCreate { get; set; } = (workspace) => new WorkspaceResponse
        {
            Id = Guid.NewGuid().ToString(),
            SessionToken = Guid.NewGuid().ToString(),
            HeartbeatInterval = TimeSpan.FromSeconds(60),
            Name = workspace.Name,
            ConnectionMode = workspace.ConnectionMode,
            AreAnonymousGuestsAllowed = workspace.AreAnonymousGuestsAllowed,
            ExpiresAt = workspace.ExpiresAt,
        };

        /// <summary>Gets or sets a function for returning a mocked GetStatus response.</summary>
        public Func<string, WorkspaceResponse> MockGetStatus { get; set; } = (workspaceId) => new WorkspaceResponse
        {
            Id = workspaceId,
            SessionToken = Guid.NewGuid().ToString(),
            HeartbeatInterval = TimeSpan.FromSeconds(60),
            Name = "test-connection",
            ConnectionMode = ConnectionMode.Auto,
            AreAnonymousGuestsAllowed = false,
            ExpiresAt = DateTime.UtcNow + TimeSpan.FromDays(1),
        };

        /// <inheritdoc/>
        public Task<WorkspaceResponse> CreateAsync(WorkspaceRequest workspace)
        {
            Requires.NotNull(MockCreate, nameof(MockCreate));
            return Task.FromResult(MockCreate(workspace));
        }

        /// <inheritdoc/>
        public Task DeleteAsync(string workspaceId)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<WorkspaceResponse> GetStatusAsync(string workspaceId)
        {
            Requires.NotNull(MockGetStatus, nameof(MockGetStatus));
            return Task.FromResult(MockGetStatus(workspaceId));
        }
    }
}
