// <copyright file="EnvironmentHardDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Hard Delete Action Beyond Recovery.
    /// </summary>
    public class EnvironmentHardDeleteAction : EnvironmentBaseItemAction<EnvironmentHardDeleteActionInput, object, bool>, IEnvironmentHardDeleteAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentHardDeleteAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="resourceBrokerHttpClient">Target resource broker http client.</param>
        /// <param name="workspaceManager">Target workspace manager.</param>
        /// <param name="environmentListAction">Target environment list action.</param>
        /// <param name="heartbeatRepository">Target environment heartbeat repository.</param>
        public EnvironmentHardDeleteAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IWorkspaceManager workspaceManager,
            IEnvironmentListAction environmentListAction,
            ICloudEnvironmentHeartbeatRepository heartbeatRepository)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            HeartbeatRepository = Requires.NotNull(heartbeatRepository, nameof(heartbeatRepository));
        }

        private ICloudEnvironmentHeartbeatRepository HeartbeatRepository { get; }
        
        /// <inheritdoc/>
        protected override string LogBaseName => "environment_hard_delete_action";

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        private IEnvironmentListAction EnvironmentListAction { get; }

        /// <inheritdoc/>
        public async Task<bool> RunAsync(Guid cloudEnvironmentId, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(cloudEnvironmentId, nameof(cloudEnvironmentId));

            var input = new EnvironmentHardDeleteActionInput(cloudEnvironmentId);

            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> RunAsync(
            Guid cloudEnvironmentId,
            Guid? computeResourceId,
            Guid? storageResourceId,
            Guid? osDiskResourceId,
            string liveshareWorkspaceId,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(cloudEnvironmentId, nameof(cloudEnvironmentId));

            var input = new EnvironmentHardDeleteActionInput(cloudEnvironmentId)
            {
                AllocatedComputeId = computeResourceId,
                AllocatedStorageId = storageResourceId,
                AllocatedOsDiskId = osDiskResourceId,
                AllocatedLiveshareWorkspaceId = liveshareWorkspaceId,
            };

            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<bool> RunCoreAsync(
            EnvironmentHardDeleteActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            var record = await FetchOrGetDefaultAsync(input, logger.NewChildLogger());

            var computeToken = input.AllocatedComputeId;
            var storageToken = input.AllocatedStorageId;
            var liveshareToken = input.AllocatedLiveshareWorkspaceId;
            var osDiskIdToken = input.AllocatedOsDiskId;
            string heartbeatIdToken = default;

            if (record?.Value != null)
            {
                // Validate location
                ValidateTargetLocation(record.Value.Location, logger);

                // Authorize
                var nonOwnerScopes = new[]
                {
                    PlanAccessTokenScopes.DeleteEnvironments,
                    PlanAccessTokenScopes.DeleteCodespaces,
                };
                EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes, logger);

                if (record.Value.Type == EnvironmentType.CloudEnvironment)
                {
                    computeToken ??= record.Value.Compute?.ResourceId;
                    storageToken ??= record.Value.Storage?.ResourceId;
                    osDiskIdToken ??= record.Value.OSDisk?.ResourceId;
                }

                liveshareToken ??= record.Value.Connection?.WorkspaceId;
                heartbeatIdToken = record.Value.HeartbeatResourceId;
            }

            await CleanupResourcesAsync(input.Id, computeToken, storageToken, osDiskIdToken, liveshareToken, heartbeatIdToken, logger);

            if (record?.Value != null)
            {
                // Attempt delete only if the environment exists.
                await EnvironmentStateManager.SetEnvironmentStateAsync(record.Value, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateTriggers.HardDeleteEnvironment, null, null, logger.NewChildLogger());
                return await Repository.DeleteAsync(record.Value.Id, logger.NewChildLogger());
            }

            // Return true if the environment is already deleted or if the delete is attempted before the record is persisted (ex: Cleanup attempt in response to creation failure)
            return true;
        }

        private async Task CleanupResourcesAsync(
            Guid environmentId,
            Guid? computeIdToken,
            Guid? storageIdToken,
            Guid? osDiskIdToken,
            string liveshareToken,
            string heartbeatIdToken,
            IDiagnosticsLogger logger)
        {
            if (storageIdToken != null)
            {
                await logger.OperationScopeAsync(
                    $"{LogBaseName}_delete_storage",
                    async (innerLogger) =>
                    {
                        innerLogger.FluentAddBaseValue(nameof(storageIdToken), storageIdToken.Value);

                        await ResourceBrokerClient.DeleteAsync(
                            environmentId,
                            storageIdToken.Value,
                            innerLogger.NewChildLogger());
                    },
                    swallowException: true);
            }

            if (computeIdToken != null)
            {
                await logger.OperationScopeAsync(
                   $"{LogBaseName}_delete_compute",
                   async (innerLogger) =>
                   {
                       innerLogger.FluentAddBaseValue(nameof(computeIdToken), computeIdToken.Value);

                       await ResourceBrokerClient.DeleteAsync(
                           environmentId,
                           computeIdToken.Value,
                           innerLogger.NewChildLogger());
                   },
                   swallowException: true);
            }

            if (osDiskIdToken != null)
            {
                await logger.OperationScopeAsync(
                   $"{LogBaseName}_delete_osdisk",
                   async (innerLogger) =>
                   {
                       innerLogger.FluentAddBaseValue(nameof(osDiskIdToken), osDiskIdToken.Value);

                       await ResourceBrokerClient.DeleteAsync(
                           environmentId,
                           osDiskIdToken.Value,
                           innerLogger.NewChildLogger());
                   },
                   swallowException: true);
            }

            if (liveshareToken != null)
            {
                await logger.OperationScopeAsync(
                   $"{LogBaseName}_delete_workspace",
                   async (innerLogger) =>
                   {
                       innerLogger.FluentAddBaseValue("ConnectionSessionId", liveshareToken);
                       await WorkspaceManager.DeleteWorkspaceAsync(liveshareToken, innerLogger.NewChildLogger());
                   },
                   swallowException: true);
            }

            if (heartbeatIdToken != null)
            {
                 await logger.OperationScopeAsync(
                   $"{LogBaseName}_delete_heartbeat_record",
                   async (innerLogger) =>
                   {
                       innerLogger.FluentAddBaseValue("HeartbeatResourceId", heartbeatIdToken);
                       await HeartbeatRepository.DeleteAsync(heartbeatIdToken, logger.NewChildLogger());
                   },
                   swallowException: true);
            }
        }
    }
}
