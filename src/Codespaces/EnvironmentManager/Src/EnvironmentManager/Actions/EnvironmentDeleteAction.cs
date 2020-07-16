// <copyright file="EnvironmentDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Delete Action.
    /// </summary>
    public class EnvironmentDeleteAction : EnvironmentBaseItemAction<EnvironmentDeleteActionInput, bool>, IEnvironmentDeleteAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDeleteAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// /// <param name="resourceBrokerHttpClient">Target resource broker http client.</param>
        /// <param name="workspaceManager">Target workspace manager.</param>
        public EnvironmentDeleteAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IResourceBrokerResourcesExtendedHttpContract resourceBrokerHttpClient,
            IWorkspaceManager workspaceManager)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            ResourceBrokerClient = Requires.NotNull(resourceBrokerHttpClient, nameof(resourceBrokerHttpClient));
            WorkspaceManager = Requires.NotNull(workspaceManager, nameof(workspaceManager));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_delete_action";

        private IResourceBrokerResourcesExtendedHttpContract ResourceBrokerClient { get; }

        private IWorkspaceManager WorkspaceManager { get; }

        /// <inheritdoc/>
        public async Task<bool> Run(string cloudEnvironmentId, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(cloudEnvironmentId, nameof(cloudEnvironmentId));

            var input = new EnvironmentDeleteActionInput
            {
                CloudEnvironment = new CloudEnvironment { Id = cloudEnvironmentId },
            };

            input.CloudEnvironment = (await FetchAsync(input, logger))?.Value;
            if (input.CloudEnvironment == null)
            {
                // Already deleted / does not exist.
                return true;
            }

            return await Run(input, logger);
        }

        /// <inheritdoc/>
        public async Task<bool> Run(CloudEnvironment cloudEnvironment, IDiagnosticsLogger logger)
        {
            Requires.NotNull(cloudEnvironment, nameof(cloudEnvironment));

            var input = new EnvironmentDeleteActionInput
            {
                CloudEnvironment = cloudEnvironment,
            };
            return await Run(input, logger);
        }

        /// <inheritdoc/>
        protected override Task<bool> RunCoreAsync(EnvironmentDeleteActionInput input, IDiagnosticsLogger logger)
        {
            Requires.NotNull(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete",
                async (childLogger) =>
                {
                    var cloudEnvironment = input.CloudEnvironment;
                    childLogger.AddCloudEnvironment(cloudEnvironment);
                    childLogger.AddVsoPlanInfo(cloudEnvironment.PlanId);

                    if (cloudEnvironment.Type == EnvironmentType.CloudEnvironment)
                    {
                        var storageIdToken = cloudEnvironment.Storage?.ResourceId;
                        if (storageIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                                $"{LogBaseName}_delete_storage",
                                async (innerLogger) =>
                                {
                                    innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(storageIdToken), storageIdToken.Value);

                                    await ResourceBrokerClient.DeleteAsync(
                                        Guid.Parse(cloudEnvironment.Id),
                                        storageIdToken.Value,
                                        innerLogger.NewChildLogger());
                                },
                                swallowException: true);
                        }

                        var computeIdToken = cloudEnvironment.Compute?.ResourceId;
                        if (computeIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                               $"{LogBaseName}_delete_compute",
                               async (innerLogger) =>
                               {
                                   innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(computeIdToken), computeIdToken.Value);

                                   await ResourceBrokerClient.DeleteAsync(
                                       Guid.Parse(cloudEnvironment.Id),
                                       computeIdToken.Value,
                                       innerLogger.NewChildLogger());
                               },
                               swallowException: true);
                        }

                        var osDiskIdToken = cloudEnvironment.OSDisk?.ResourceId;
                        if (osDiskIdToken != null)
                        {
                            await childLogger.OperationScopeAsync(
                               $"{LogBaseName}_delete_osdisk",
                               async (innerLogger) =>
                               {
                                   innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                        .FluentAddBaseValue(nameof(osDiskIdToken), osDiskIdToken.Value);

                                   await ResourceBrokerClient.DeleteAsync(
                                       Guid.Parse(cloudEnvironment.Id),
                                       osDiskIdToken.Value,
                                       innerLogger.NewChildLogger());
                               },
                               swallowException: true);
                        }
                    }

                    if (cloudEnvironment.Connection?.WorkspaceId != null)
                    {
                        await childLogger.OperationScopeAsync(
                            $"{LogBaseName}_delete_workspace",
                            async (innerLogger) =>
                            {
                                innerLogger.FluentAddBaseValue(nameof(cloudEnvironment.Id), cloudEnvironment.Id)
                                    .FluentAddBaseValue("ConnectionSessionId", cloudEnvironment.Connection?.WorkspaceId);

                                await WorkspaceManager.DeleteWorkspaceAsync(cloudEnvironment.Connection.WorkspaceId, innerLogger.NewChildLogger());
                            },
                            swallowException: true);
                    }

                    cloudEnvironment = (await FetchAsync(input, logger))?.Value;
                    if (cloudEnvironment != null)
                    {
                        // Attempt delete only if the environment exists.
                        await EnvironmentStateManager.SetEnvironmentStateAsync(cloudEnvironment, CloudEnvironmentState.Deleted, CloudEnvironmentStateUpdateTriggers.DeleteEnvironment, null, null, childLogger.NewChildLogger());
                        return await Repository.DeleteAsync(cloudEnvironment.Id, childLogger.NewChildLogger());
                    }

                    // Return true if the environment is already deleted or if the delete is attempted before the record is persisted (ex: Cleanup attempt in response to creation failure)
                    return true;
                });
        }

        /// <inheritdoc/>
        protected override EnvironmentTransition BuildTransition(CloudEnvironment model)
        {
            throw new NotSupportedException();
        }
    }
}
