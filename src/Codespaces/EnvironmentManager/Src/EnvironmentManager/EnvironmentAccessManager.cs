// <copyright file="EnvironmentAccessManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Diagnostics;
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
            var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;

            bool? isComputeAuthorized = IsComputeAuthorized(environment);
            if (isComputeAuthorized != null)
            {
                if (isComputeAuthorized == true)
                {
                    // Identities that has access to the compute have full access to the environment (VMToken)
                    return;
                }

                errorCode = UnauthorizedComputeId;
            }
            else if (CurrentUserProvider.Identity.IsPlanAuthorized(environment.PlanId) == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                errorCode = UnauthorizedPlanId;
            }
            else if (CurrentUserProvider.Identity.IsEnvironmentAuthorized(environment.Id) == false)
            {
                // Users with explicit access to different env(s) do not have access to this one.
                errorCode = UnauthorizedEnvironmentId;
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

                errorCode = UnauthorizedPlanScope;
            }
            else
            {
                errorCode = UnauthorizedEnvironmentUser;
            }

            logger.LogWarning(errorCode);

            throw new UnauthorizedAccessException(errorCode);
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
            if (isPlanAuthorized == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                errorCode = UnauthorizedPlanId;
            }
            else if (identity.IsEnvironmentAuthorized(null) == false)
            {
                // Users with explicit access to env(s) do not have access to the whole plan.
                errorCode = UnauthorizedEnvironmentId;
            }
            else if (isScopeAuthorized == true)
            {
                // The user has the explicit required scope.
                return;
            }
            else if (isScopeAuthorized == false)
            {
                // Users with a scoped access token must have the required scope.
                errorCode = UnauthorizedPlanScope;
            }
            else if (plan.UserId != null)
            {
                // Users without a scoped access token must be the owner of the plan
                // (if the plan has an owner).
                var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;
                if (!currentUserIdSet.EqualsAny(plan.UserId))
                {
                    errorCode = UnauthorizedPlanUser;
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
            }
            else
            {
                return;
            }

            logger.LogWarning(errorCode);

            throw new UnauthorizedAccessException(errorCode);
        }

        private bool? IsComputeAuthorized(CloudEnvironment environment)
        {
            var resourceId = default(string);
            if (environment.Type == EnvironmentType.CloudEnvironment)
            {
                resourceId = environment.Compute?.ResourceId.ToString();
            }
            else if (environment.Type == EnvironmentType.StaticEnvironment)
            {
                // For static environments resourceId on the VmToken is the environmentId.
                resourceId = environment.Id;
            }

            if (resourceId != default)
            {
                return CurrentUserProvider.Identity.IsComputeAuthorized(resourceId);
            }

            return null;
        }
    }
}
