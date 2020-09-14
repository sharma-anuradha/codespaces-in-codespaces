// <copyright file="WorkspaceManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareWorkspace.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Implements workspace manager.
    /// </summary>
    public class WorkspaceManager : IWorkspaceManager
    {
        private const int PersistentSessionExpiresInDays = 30;
        private static readonly string LogBaseName = nameof(WorkspaceManager);

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceManager"/> class.
        /// </summary>
        /// <param name="workspaceRepository">work space repository.</param>
        public WorkspaceManager(IWorkspaceRepository workspaceRepository)
        {
            WorkspaceRepository = Requires.NotNull(workspaceRepository, nameof(workspaceRepository));
        }

        private IWorkspaceRepository WorkspaceRepository { get; }

        /// <inheritdoc/>
        public async Task<ConnectionInfo> CreateWorkspaceAsync(
            EnvironmentType environmentType,
            string environmentId,
            Guid computeResourceId,
            Uri connectionServiceUri,
            string sessionPath,
            string emailAddress,
            string profileId,
            bool scopeForProfileId,
            string authToken,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_create_workspace",
                async (childLogger) =>
                {
                    Requires.NotNullOrEmpty(environmentId, nameof(environmentId));
                    Requires.NotNull(connectionServiceUri, nameof(connectionServiceUri));

                    var workspaceRequest = new WorkspaceRequest()
                    {
                        Name = environmentId.ToString(),
                        ConnectionMode = ConnectionMode.Auto,
                        AreAnonymousGuestsAllowed = false,
                        ExpiresAt = DateTime.UtcNow.AddDays(PersistentSessionExpiresInDays),
                    };

                    var workspaceResponse = await WorkspaceRepository.CreateAsync(workspaceRequest, authToken, childLogger);
                    if (string.IsNullOrWhiteSpace(workspaceResponse.Id))
                    {
                        childLogger
                            .AddEnvironmentId(environmentId)
                            .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateWorkspaceAsync)), "Failed to create workspace");
                        return null;
                    }

                    string[] guestUsers = null;
                    string[] guestUserIds = null;

                    if (scopeForProfileId)
                    {
                        if (!string.IsNullOrWhiteSpace(profileId))
                        {
                            guestUserIds = new string[] { profileId };
                        }
                    }
                    else
                    {
                        // Only add email if it is not github. True for non github customers.
                        if (!string.IsNullOrWhiteSpace(emailAddress))
                        {
                            // Note: Going forward this will not be passed around.
                            guestUsers = new string[] { emailAddress };
                        }
                    }

                    var invitationLinkInfo = new SharedInvitationLinkInfo()
                    {
                        WorkspaceId = workspaceResponse.Id,
                        GuestUsers = guestUsers,
                        GuestUserIds = guestUserIds,
                    };

                    LogInvitationInfo(invitationLinkInfo, logger);

                    var workspaceInvitationId = await WorkspaceRepository.GetInvitationLinkAsync(invitationLinkInfo, authToken, childLogger);
                    if (string.IsNullOrWhiteSpace(workspaceInvitationId))
                    {
                        childLogger
                            .AddEnvironmentId(workspaceResponse.Id)
                            .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateWorkspaceAsync)), "Failed to create invitation id");
                        return null;
                    }

                    var connectionInfo = new ConnectionInfo
                    {
                        ConnectionServiceUri = connectionServiceUri.AbsoluteUri,
                        ConnectionComputeId = computeResourceId.ToString(),
                        ConnectionComputeTargetId = environmentType.ToString(),
                        ConnectionSessionId = workspaceInvitationId,
                        WorkspaceId = workspaceResponse.Id,
                        ConnectionSessionPath = sessionPath,
                    };

                    childLogger
                        .AddNewConnectionInfo(connectionInfo)
                        .LogInfo($"{LogBaseName}_created_new_workspace");

                    return connectionInfo;
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public async Task<WorkspaceResponse> GetWorkspaceStatusAsync(
            string workspaceId,
            IDiagnosticsLogger logger)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_status_workspace",
                async (childLogger) =>
                {
                    return await WorkspaceRepository.GetStatusAsync(workspaceId, childLogger);
                },
                swallowException: false);
        }

        /// <inheritdoc/>
        public async Task DeleteWorkspaceAsync(string workspaceId, IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_delete_workspace",
                async (childLogger) =>
                {
                    await WorkspaceRepository.DeleteAsync(workspaceId, childLogger);
                },
                swallowException: false);
        }

        /// <summary>
        /// Add logging fields for Invitation link info instance.
        /// </summary>
        /// <param name="invitationLinkInfo">The connection info.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The logger.</returns>
        private IDiagnosticsLogger LogInvitationInfo(SharedInvitationLinkInfo invitationLinkInfo, IDiagnosticsLogger logger)
        {
            const string InvitationHasGuestEmail = nameof(InvitationHasGuestEmail);
            const string InvitationHasGuestProfileId = nameof(InvitationHasGuestProfileId);

            Requires.NotNull(logger, nameof(logger));

            if (invitationLinkInfo != default)
            {
                logger
                    .FluentAddValue(InvitationHasGuestEmail, invitationLinkInfo?.GuestUsers?.Length > 0)
                    .FluentAddValue(InvitationHasGuestProfileId, invitationLinkInfo?.GuestUserIds?.Length > 0);
            }

            return logger;
        }
    }
}
