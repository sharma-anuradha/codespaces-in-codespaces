// <copyright file="EnvironmentDeleteRestoreAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts.Actions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment Restore Action.
    /// </summary>
    public class EnvironmentDeleteRestoreAction : EnvironmentBaseItemAction<Guid, object, CloudEnvironment>, IEnvironmentDeleteRestoreAction
    {
        // One thousand is an arbitrary number. Have to stop somewhere
        private const int MaxNumberOfAttemptsToResolveNameConflict = 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentDeleteRestoreAction"/> class.
        /// </summary>
        /// <param name="environmentStateManager">Target environment state manager.</param>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentAccessManager">Target environment access manager.</param>
        /// <param name="environmentListAction">Environment List Action.</param>
        /// <param name="planManager">Target plan manager.</param>
        /// <param name="environmentActionValidator">Environment action validator.</param>
        /// /// <param name="environmentManagerSettings">Environment manager settings.</param>
        public EnvironmentDeleteRestoreAction(
            IEnvironmentStateManager environmentStateManager,
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentListAction environmentListAction,
            IPlanManager planManager,
            IEnvironmentActionValidator environmentActionValidator,
            EnvironmentManagerSettings environmentManagerSettings)
            : base(environmentStateManager, repository, currentLocationProvider, currentUserProvider, controlPlaneInfo, environmentAccessManager)
        {
            EnvironmentListAction = Requires.NotNull(environmentListAction, nameof(environmentListAction));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            EnvironmentActionValidator = Requires.NotNull(environmentActionValidator, nameof(environmentActionValidator));
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_delete_restore_action";

        private IEnvironmentListAction EnvironmentListAction { get; }

        private IPlanManager PlanManager { get; }

        private IEnvironmentActionValidator EnvironmentActionValidator { get; }

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <inheritdoc/>
        protected async override Task<CloudEnvironment> RunCoreAsync(Guid input, object transientState, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(input, nameof(input));
            Requires.NotNull(logger, nameof(logger));

            return await logger.OperationScopeAsync(
                $"{LogBaseName}_delete_restore",
                async (childLogger) =>
                {
                    var environmentSoftDeleteEnabled = await EnvironmentManagerSettings.EnvironmentSoftDeleteEnabled(logger.NewChildLogger());

                    // Return Forbidden if the soft delete feature is not enabled.
                    if (environmentSoftDeleteEnabled != true)
                    {
                        throw new ForbiddenException((int)MessageCodes.FeatureDisabled);
                    }

                    var record = await FetchAsync(input, logger.NewChildLogger());
                    ValidateTargetLocation(record.Value.Location, logger);

                    var cloudEnvironment = record.Value;
                    logger.AddCloudEnvironment(cloudEnvironment);

                    // Pull Plan
                    VsoPlanInfo.TryParse(record.Value.PlanId, out var plan);
                    var planDetails = await PlanManager.GetAsync(plan, logger);

                    if (planDetails == default)
                    {
                        throw new ForbiddenException((int)MessageCodes.PlanDoesNotExist);
                    }

                    logger.AddVsoPlan(planDetails);

                    // Authorize Access
                    var requiredScopes = new[]
                    {
                        PlanAccessTokenScopes.WriteEnvironments,
                        PlanAccessTokenScopes.WriteCodespaces,
                    };

                    EnvironmentAccessManager.AuthorizePlanAccess(planDetails, requiredScopes, null, logger);
                    EnvironmentAccessManager.AuthorizeEnvironmentAccess(record.Value, requiredScopes, logger);

                    // Core Validation
                    ValidateTargetLocation(cloudEnvironment.Location, logger);

                    var environmentsInPlan = await EnvironmentListAction.RunAsync(
                        record.Value.PlanId,
                        name: null,
                        identity: null,
                        userIdSet: null,
                        EnvironmentListType.ActiveEnvironments,
                        logger.NewChildLogger());
                    await ValidateSubscriptionAndPlanAsync(cloudEnvironment, planDetails, environmentsInPlan, logger);

                    if (record.Value.IsDeleted != true)
                    {
                        return record.Value;
                    }

                    // Validate against existing environments
                    if (!IsEnvironmentNameAvailable(cloudEnvironment.FriendlyName, environmentsInPlan))
                    {
                        bool nameChanged = false;

                        for (int number = 1; number <= MaxNumberOfAttemptsToResolveNameConflict; number++)
                        {
                            var newFriendlyName = $"{cloudEnvironment.FriendlyName}_restored_{number}";
                            if (IsEnvironmentNameAvailable(newFriendlyName, environmentsInPlan))
                            {
                                cloudEnvironment.FriendlyName = newFriendlyName;
                                nameChanged = true;
                                break;
                            }
                        }

                        if (nameChanged != true)
                        {
                            throw new ConflictException((int)MessageCodes.UnableToResolveEnvironmentNameConflict);
                        }
                    }

                    // Reseting the delete flag only as environment should already have a valid environment state.
                    cloudEnvironment.IsDeleted = false;

                    // Todo: Need to send new state to billing before we make the soft delete/restore feature public.

                    // Update the database state.
                    cloudEnvironment = await Repository.UpdateAsync(cloudEnvironment, childLogger.NewChildLogger());
                    record.ReplaceAndResetTransition(cloudEnvironment);

                    return cloudEnvironment;
                });
        }

        /// <summary>
        /// Checks if a name matches the name of any existing environments in a plan.
        /// </summary>
        /// <returns>
        /// Currently every name must be unique within the plan, even across multiple users.
        /// </returns>
        private static bool IsEnvironmentNameAvailable(
            string name,
            IEnumerable<CloudEnvironment> environmentsInPlan)
        {
            return !environmentsInPlan.Any(
                (env) => string.Equals(env.FriendlyName, name, StringComparison.InvariantCultureIgnoreCase));
        }

        private async Task ValidateSubscriptionAndPlanAsync(
            CloudEnvironment cloudEnvironment, VsoPlan planDetails, IEnumerable<CloudEnvironment> environmentsInPlan, IDiagnosticsLogger logger)
        {
            await EnvironmentActionValidator.ValidateSubscriptionAndQuotaAsync(cloudEnvironment.SkuName, environmentsInPlan, planDetails.Plan.Subscription, planDetails.Partner, logger.NewChildLogger());
        }
    }
}
