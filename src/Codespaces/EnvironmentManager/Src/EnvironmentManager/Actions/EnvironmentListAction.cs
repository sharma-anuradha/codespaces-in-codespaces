// <copyright file="EnvironmentListAction.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
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
    /// Environment List Action.
    /// </summary>
    public class EnvironmentListAction : EntityListAction<ListEnvironmentActionInput, CloudEnvironment, ICloudEnvironmentRepository>, IEnvironmentListAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentListAction"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="systemActionGetProvider">Target system action get provider.</param>
        public EnvironmentListAction(
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo)
            : base(repository, currentLocationProvider, currentUserProvider, controlPlaneInfo)
        {
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_list_action";

        /// <inheritdoc/>
        public Task<IEnumerable<CloudEnvironment>> Run(
            string planId, string name, UserIdSet userIdSet, IDiagnosticsLogger logger)
        {
            var identity = CurrentUserProvider.Identity;

            // Build input
            var input = new ListEnvironmentActionInput { Name = name };

            // Determine plan id
            // In the case of a authentication using a plan access token, infer the plan from the token if not set
            input.PlanId = planId ?? identity.AuthorizedPlan;

            if (input.PlanId != null)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                if (identity.IsPlanAuthorized(input.PlanId) == false)
                {
                    throw new UnauthorizedAccessException(VsoClaimsIdentity.IsPlanAuthorizedErrorCode);
                }

                // Determine user id set
                if (CurrentUserProvider.Identity.IsAnyScopeAuthorized(
                    PlanAccessTokenScopes.ReadEnvironments,
                    PlanAccessTokenScopes.ReadCodespaces) == true)
                {
                    // A user with this explicit scope is authorized to list all users' environments in the plan,
                    // hence do not filter by owner unless a userIdSet is provided explicitly
                    input.UserIdSet = userIdSet;
                }
                else if (CurrentUserProvider.Identity.IsAnyScopeAuthorized(
                    PlanAccessTokenScopes.WriteEnvironments,
                    PlanAccessTokenScopes.WriteCodespaces) != false)
                {
                    // A user with only write scope (or an unscoped token) is authorized only to list their own environments.
                    input.UserIdSet = CurrentUserProvider.CurrentUserIdSet;
                }
                else
                {
                    // NOTE: This is a weird case... need to talk about better ways of doing this.
                    throw new UnauthorizedAccessException(VsoClaimsIdentity.IsScopeAuthorizedErrorCode);
                }
            }
            else
            {
                if (identity.IsSuperuser())
                {
                    // Only superuser can read environments for all users across plans.
                    input.UserIdSet = userIdSet ?? CurrentUserProvider.CurrentUserIdSet;
                }
                else if (CurrentUserProvider.CurrentUserIdSet != null)
                {
                    // If plan is null, user can only read their own environments
                    input.UserIdSet = CurrentUserProvider.CurrentUserIdSet;
                }
                else
                {
                    throw new UnauthorizedAccessException(VsoClaimsIdentity.IsScopeAuthorizedErrorCode);
                }
            }

            return Run(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<CloudEnvironment>> RunCoreAsync(ListEnvironmentActionInput input, IDiagnosticsLogger logger)
        {
            var environments = Enumerable.Empty<CloudEnvironment>();
            var environmentNameInLowerCase = input.Name?.Trim()?.ToLowerInvariant();

            // The code is written like this to optimize the CosmosDB lookups - consider that optimization if modifying it.
            if (input.UserIdSet == null)
            {
                if (input.PlanId != null)
                {
                    // Query by planId
                    if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                    {
                        environments = await Repository.GetWhereAsync(
                            (cloudEnvironment) => cloudEnvironment.PlanId == input.PlanId &&
                                cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                            logger.NewChildLogger());
                    }
                    else
                    {
                        environments = await Repository.GetWhereAsync(
                            (cloudEnvironment) => cloudEnvironment.PlanId == input.PlanId,
                            logger.NewChildLogger());
                    }
                }
            }
            else if (input.PlanId == null)
            {
                // Query by userIdSet
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    environments = await Repository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == input.UserIdSet.CanonicalUserId ||
                                cloudEnvironment.OwnerId == input.UserIdSet.ProfileId) &&
                            cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                        logger.NewChildLogger());
                }
                else
                {
                    environments = await Repository.GetWhereAsync(
                        (cloudEnvironment) => cloudEnvironment.OwnerId == input.UserIdSet.CanonicalUserId ||
                            cloudEnvironment.OwnerId == input.UserIdSet.ProfileId,
                        logger.NewChildLogger());
                }
            }
            else
            {
                // Query by planId and userIdSet
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    environments = await Repository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == input.UserIdSet.CanonicalUserId ||
                                cloudEnvironment.OwnerId == input.UserIdSet.ProfileId) &&
                            cloudEnvironment.PlanId == input.PlanId &&
                            cloudEnvironment.FriendlyNameInLowerCase == environmentNameInLowerCase,
                        logger.NewChildLogger());
                }
                else
                {
                    environments = await Repository.GetWhereAsync(
                        (cloudEnvironment) => (cloudEnvironment.OwnerId == input.UserIdSet.CanonicalUserId ||
                                cloudEnvironment.OwnerId == input.UserIdSet.ProfileId) &&
                            cloudEnvironment.PlanId == input.PlanId,
                        logger.NewChildLogger());
                }
            }

            // Filter out records that they don't have access to
            var environmentIds = CurrentUserProvider.Identity?.AuthorizedEnvironments;
            if (environmentIds != null)
            {
                // The user has a token that limits access to specific environment(s).
                environments = environments.Where((e) => environmentIds.Contains(e.Id));
            }

            return environments;
        }
    }
}
