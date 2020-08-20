// <copyright file="EnvironmentBaseItemAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Item Base Action which supports a generic result typoe.
    /// </summary>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <typeparam name="TState">Transitent state to track properties required for exception handling.</typeparam>
    /// <typeparam name="TResult">TResult type.</typeparam>
    public abstract class EnvironmentBaseItemAction<TInput, TState, TResult> :
        EntityItemAction<TInput, TState, TResult, EnvironmentTransition, ICloudEnvironmentRepository, CloudEnvironment>,
        IEnvironmentBaseItemAction<TInput, TState, TResult>
        where TState : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentBaseItemAction{TInput, TState, TResult}"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        protected EnvironmentBaseItemAction(
                IEnvironmentStateManager environmentStateManager,
                ICloudEnvironmentRepository repository,
                ICurrentLocationProvider currentLocationProvider,
                ICurrentUserProvider currentUserProvider,
                IControlPlaneInfo controlPlaneInfo,
                IEnvironmentAccessManager environmentAccessManager)
                : base(repository, currentLocationProvider, currentUserProvider, controlPlaneInfo)
        {
            EnvironmentStateManager = Requires.NotNull(environmentStateManager, nameof(environmentStateManager));
            EnvironmentAccessManager = Requires.NotNull(environmentAccessManager, nameof(environmentAccessManager));
        }

        /// <summary>
        /// Gets the Environment State Manager.
        /// </summary>
        protected IEnvironmentStateManager EnvironmentStateManager { get; }

        /// <summary>
        /// Gets environment access manager.
        /// </summary>
        protected IEnvironmentAccessManager EnvironmentAccessManager { get; }

        /// <inheritdoc/>
        protected override string EntityName => "Environment";

        /// <inheritdoc/>
        protected override async Task<EnvironmentTransition> FetchOrGetDefaultAsync(
            TInput input,
            IDiagnosticsLogger logger)
        {
            // Fetch record
            var record = await base.FetchOrGetDefaultAsync(input, logger);

            if (record != null)
            {
                ValidateAndAuthorizeRecord(record, logger);

                // Redirect if the Codespace is in the wrong region.
                ValidateTargetLocation(record.Value.Location, logger);
            }

            return record;
        }

        /// <inheritdoc/>
        protected override async Task<EnvironmentTransition> FetchAsync(
            TInput input,
            IDiagnosticsLogger logger)
        {
            // Fetch record
            var record = await base.FetchAsync(input, logger);

            if (record != null)
            {
                ValidateAndAuthorizeRecord(record, logger);

                // Redirect if the Codespace is in the wrong region.
                ValidateTargetLocation(record.Value.Location, logger);
            }

            return record;
        }

        /// <inheritdoc/>
        protected override EnvironmentTransition BuildTransition(CloudEnvironment model)
        {
            return new EnvironmentTransition(model);
        }

        private void ValidateAndAuthorizeRecord(EnvironmentTransition record, IDiagnosticsLogger logger)
        {
            // Apply logging
            logger.AddCloudEnvironment(record.Value);
            logger.AddVsoPlanInfo(record.Value.PlanId);

            // Authorize Access
            var nonOwnerScopes = new[]
            {
                PlanAccessTokenScopes.ReadEnvironments,
                PlanAccessTokenScopes.ReadCodespaces,
            };
            EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, nonOwnerScopes, logger.NewChildLogger());
        }
    }
}
