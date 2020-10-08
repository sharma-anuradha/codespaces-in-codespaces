// <copyright file="EnvironmentSoftDeleteAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Soft Delete Action Beyond Recovery.
    /// </summary>
    public class EnvironmentSoftDeleteAction : EnvironmentBaseItemAction<Guid, object, CloudEnvironment>, IEnvironmentSoftDeleteAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSoftDeleteAction"/> class.
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
        /// <param name="environmentSuspendAction">Target environment suspend action.</param>
        public EnvironmentSoftDeleteAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentSuspendAction environmentSuspendAction)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            EnvironmentSuspendAction = Requires.NotNull(environmentSuspendAction, nameof(environmentSuspendAction));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_soft_delete_action";

        private IEnvironmentSuspendAction EnvironmentSuspendAction { get; }

        /// <inheritdoc/>
        protected override Task<CloudEnvironment> RunCoreAsync(
            Guid input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return logger.OperationScopeAsync(
                $"{LogBaseName}_soft_delete",
                async (childLogger) =>
                {
                    var record = await FetchAsync(input, logger.NewChildLogger());

                    ValidateTargetLocation(record.Value.Location, logger);

                    // Authorize
                    var nonOwnerScopes = new[]
                    {
                        PlanAccessTokenScopes.DeleteEnvironments,
                        PlanAccessTokenScopes.DeleteCodespaces,
                    };
                    EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes, logger);

                    if (record.Value.IsDeleted == true)
                    {
                        return record.Value;
                    }

                    var lastDeleted = DateTime.UtcNow;
                    record.PushTransition((environment) =>
                    {
                        environment.IsDeleted = true;
                        environment.LastDeleted = lastDeleted;
                    });

                    await Repository.UpdateTransitionAsync("cloudenvironment", record, logger);

                    // Added the actual suspend action call in the "try" block so that we always emit "Deleted" billing event even if
                    // suspend call fails and throws exception. As environment was marked as deleted in the previous "update" statement,
                    // it will ensure that billing is always stopped when we mark the environment as deleted.
                    try
                    {
                        if (record.Value.Compute != null)
                        {
                            await EnvironmentSuspendAction.RunAsync(
                                Guid.Parse(record.Value.Id),
                                record.Value.Compute.ResourceId,
                                logger.NewChildLogger());
                        }
                    }
                    finally
                    {
                        await EnvironmentStateManager.SetEnvironmentStateAsync(
                            record.Value,
                            CloudEnvironmentState.Deleted,
                            CloudEnvironmentStateUpdateTriggers.SoftDeleteEnvironment,
                            null,
                            null,
                            childLogger.NewChildLogger());
                    }

                    return record.Value;
                });
        }
    }
}
