// <copyright file="EnvironmentAccessManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <inheritdoc/>
    public class EnvironmentAccessManager : IEnvironmentAccessManager
    {
        private const string UnauthorizedPlanId = "unauthorized_plan_id";
        private const string UnauthorizedEnvironmentId = "unauthorized_environment_id";
        private const string UnauthorizedPlanScope = "unauthorized_plan_scope";
        private const string UnauthorizedPlanUser = "unauthorized_plan_user";
        private const string UnauthorizedEnvironmentUser = "unauthorized_environment_user";
        private const string UnauthorizedComputeId = "unauthorized_environment_compute_id";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentAccessManager"/> class.
        /// </summary>
        /// <param name="currentUserProvider">Current user provider.</param>
        public EnvironmentAccessManager(ICurrentUserProvider currentUserProvider)
        {
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <inheritdoc/>
        public void AuthorizeEnvironmentAccess(
            CloudEnvironment environment,
            string[] nonOwnerScopes,
            IDiagnosticsLogger logger)
        {
            string errorCode;
            string errorMessage;
            var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;

            bool? isComputeAuthorized = IsComputeAuthorized(environment, out string computeResource);
            if (isComputeAuthorized != null)
            {
                if (isComputeAuthorized == true)
                {
                    // Identities that has access to the compute have full access to the environment (VMToken)
                    return;
                }

                errorCode = UnauthorizedComputeId;
                errorMessage = "Authorized compute resource " +
                    $"'{CurrentUserProvider.Identity.AuthorizedComputeId}' does not match target '{computeResource}'";
            }
            else if (CurrentUserProvider.Identity.IsPlanAuthorized(environment.PlanId) == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                errorCode = UnauthorizedPlanId;
                errorMessage = "Authorized plan resource " +
                    $"'{CurrentUserProvider.Identity.AuthorizedPlan}' does not match target '{environment.PlanId}'";
            }
            else if (CurrentUserProvider.Identity.IsEnvironmentAuthorized(environment.Id) == false)
            {
                // Users with explicit access to different env(s) do not have access to this one.
                var authorizedEnvironments = string.Join(
                    ", ", CurrentUserProvider.Identity.AuthorizedEnvironments ?? Array.Empty<string>());
                errorCode = UnauthorizedEnvironmentId;
                errorMessage = "Authorized environment(s) " +
                    $"'[{authorizedEnvironments}]' does not include target '{environment.Id}'";
            }
            else if (currentUserIdSet != default &&
                     currentUserIdSet.EqualsAny(environment.OwnerId) &&
                     CurrentUserProvider.Identity.IsAnyScopeAuthorized(
                        PlanAccessTokenScopes.WriteEnvironments,
                        PlanAccessTokenScopes.WriteCodespaces) != false)
            {
                // Users with write access to a plan (or an unscoped access token)
                // have full access to their own environments in the plan.
                return;
            }
            else if (nonOwnerScopes != null)
            {
                if (CurrentUserProvider.Identity.IsAnyScopeAuthorized(nonOwnerScopes) == true)
                {
                    // Users with certain explicit scopes can have limited access
                    // to environments they don't own in the plan.
                    return;
                }

                var authorizedScopes = string.Join(
                    ", ", CurrentUserProvider.Identity.Scopes ?? Array.Empty<string>());
                var nonOwnerScopesList = string.Join(", ", nonOwnerScopes ?? Array.Empty<string>());
                errorCode = UnauthorizedPlanScope;
                errorMessage = "Authorized scopes " +
                    $"'[{authorizedScopes}]' does not include any required scope '[{nonOwnerScopesList}]'";
            }
            else
            {
                errorCode = UnauthorizedEnvironmentUser;
                errorMessage = $"User is not the owner of environment '{environment.Id}'.";
            }

            logger.FluentAddValue("ErrorMessage", errorMessage).LogWarning(errorCode);
            throw new UnauthorizedAccessException(errorMessage);
        }

        /// <inheritdoc/>
        public void AuthorizePlanAccess(
            VsoPlan plan,
            string[] requiredScopes,
            VsoClaimsIdentity identity,
            IDiagnosticsLogger logger)
        {
            identity ??= CurrentUserProvider.Identity;

            var isPlanAuthorized = identity.IsPlanAuthorized(plan.Plan.ResourceId);
            var isScopeAuthorized = identity.IsAnyScopeAuthorized(requiredScopes);

            string errorCode;
            string errorMessage;
            if (isPlanAuthorized == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                errorCode = UnauthorizedPlanId;
                errorMessage = "Authorized plan resource " +
                    $"'{CurrentUserProvider.Identity.AuthorizedPlan}' does not match target '{plan.Plan.ResourceId}'";
            }
            else if (identity.IsEnvironmentAuthorized(null) == false)
            {
                // Users with explicit access to env(s) do not have access to the whole plan.
                var authorizedEnvironments = string.Join(
                    ", ", CurrentUserProvider.Identity.AuthorizedEnvironments ?? Array.Empty<string>());
                errorCode = UnauthorizedEnvironmentId;
                errorMessage = "User is authorized to access environment(s) " +
                    $"'[{authorizedEnvironments}]' but not the plan '{plan.Plan.ResourceId}'";
            }
            else if (isScopeAuthorized == true)
            {
                // The user has the explicit required scope.
                return;
            }
            else if (isScopeAuthorized == false)
            {
                // Users with a scoped access token must have the required scope.
                var authorizedScopes = string.Join(
                    ", ", CurrentUserProvider.Identity.Scopes ?? Array.Empty<string>());
                var requiredScopesList = string.Join(", ", requiredScopes ?? Array.Empty<string>());
                errorCode = UnauthorizedPlanScope;
                errorMessage = "Authorized scopes " +
                    $"'[{authorizedScopes}]' does not include any required scope '[{requiredScopesList}]'";
            }
            else if (plan.UserId != null)
            {
                // Users without a scoped access token must be the owner of the plan
                // (if the plan has an owner).
                var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;
                if (!currentUserIdSet.EqualsAny(plan.UserId))
                {
                    errorCode = UnauthorizedPlanUser;
                    errorMessage = $"User is not the owner of plan '{plan.Plan.ResourceId}'";
                }
                else
                {
                    return;
                }
            }
            else if (isPlanAuthorized != true)
            {
                // Users must have explicit authorization for unowned plans.
                errorCode = UnauthorizedPlanId;
                errorMessage = $"No explicit authorization for plan '{plan.Plan.ResourceId}'";
            }
            else
            {
                return;
            }

            logger.FluentAddValue("ErrorMessage", errorMessage).LogWarning(errorCode);
            throw new UnauthorizedAccessException(errorMessage);
        }

        private bool? IsComputeAuthorized(CloudEnvironment environment, out string resourceId)
        {
            resourceId = default;
            if (environment.Type == EnvironmentType.CloudEnvironment)
            {
                resourceId = environment.Compute?.ResourceId.ToString();
            }
            else if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                // For static environments resourceId on the VmToken is the environmentId.
                resourceId = environment.Id;
            }

            return CurrentUserProvider.Identity.IsComputeAuthorized(resourceId);
        }
    }
}
