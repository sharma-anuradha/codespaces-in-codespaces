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
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Actions
{
    /// <summary>
    /// Environment List Action.
    /// </summary>
    public class EnvironmentListAction : EntityListAction<ListEnvironmentActionInput, object, CloudEnvironment, ICloudEnvironmentRepository>, IEnvironmentListAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentListAction"/> class.
        /// </summary>
        /// <param name="repository">Target repository.</param>
        /// <param name="currentLocationProvider">Target current location provider.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        /// <param name="controlPlaneInfo">Target control plane info.</param>
        /// <param name="environmentManagerSettings">Target environment manager settings.</param>
        public EnvironmentListAction(
            ICloudEnvironmentRepository repository,
            ICurrentLocationProvider currentLocationProvider,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            EnvironmentManagerSettings environmentManagerSettings)
            : base(repository, currentLocationProvider, currentUserProvider, controlPlaneInfo)
        {
            EnvironmentManagerSettings = Requires.NotNull(environmentManagerSettings, nameof(environmentManagerSettings));
        }

        /// <inheritdoc/>
        protected override string LogBaseName => "environment_list_action";

        private EnvironmentManagerSettings EnvironmentManagerSettings { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<CloudEnvironment>> RunAsync(
            string planId,
            string name,
            VsoClaimsIdentity identity,
            UserIdSet userIdSet,
            EnvironmentListType environmentListType,
            IDiagnosticsLogger logger)
        {
            identity ??= CurrentUserProvider.Identity;

            // Build input
            var input = new ListEnvironmentActionInput { Name = name, EnvironmentListType = environmentListType };

            // Determine plan id
            // In the case of a authentication using a plan access token, infer the plan from the token if not set
            input.PlanId = planId ?? identity.AuthorizedPlan;

            if (input.PlanId != null)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                if (identity.IsPlanAuthorized(input.PlanId) == false)
                {
                    string errorMessage = "Authorized plan resource " +
                        $"'{identity.AuthorizedPlan}' does not match target '{input.PlanId}'";
                    logger.FluentAddValue("ErrorMessage", errorMessage)
                        .LogWarning(EnvironmentAccessManager.UnauthorizedPlanId);
                    throw new UnauthorizedAccessException(errorMessage);
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
                    var authorizedScopes = string.Join(
                        ", ", CurrentUserProvider.Identity.Scopes ?? Array.Empty<string>());
                    string errorMessage = $"Authorized scopes '[{authorizedScopes}]' do not " +
                        "grant access to list codespaces in the plan.'";
                    logger.FluentAddValue("ErrorMessage", errorMessage)
                        .LogWarning(EnvironmentAccessManager.UnauthorizedPlanScope);
                    throw new UnauthorizedAccessException(errorMessage);
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
                    string errorMessage = "Missing current user identity.";
                    logger.FluentAddValue("ErrorMessage", errorMessage)
                        .LogWarning(EnvironmentAccessManager.UnauthorizedEnvironmentUser);
                    throw new UnauthorizedAccessException(errorMessage);
                }
            }

            return await RunAsync(input, logger);
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<CloudEnvironment>> RunCoreAsync(
            ListEnvironmentActionInput input,
            object transientState,
            IDiagnosticsLogger logger)
        {
            var environments = Enumerable.Empty<CloudEnvironment>();
            var environmentNameInLowerCase = input.Name?.Trim()?.ToLowerInvariant();

            // The code is written like this to optimize the CosmosDB lookups - consider that optimization if modifying it.
            if (input.UserIdSet == null)
            {
                // Query by planId
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    environments = await Repository.ListAsync(input.PlanId, environmentNameInLowerCase, logger);
                }
                else
                {
                    environments = await Repository.ListAsync(input.PlanId, logger);
                }
            }
            else if (input.PlanId == null)
            {
                // Query by userIdSet
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    environments = await Repository.ListAsync(input.UserIdSet, environmentNameInLowerCase, logger);
                }
                else
                {
                    environments = await Repository.ListAsync(input.UserIdSet, logger);
                }
            }
            else
            {
                // Query by planId and userIdSet
                if (!string.IsNullOrEmpty(environmentNameInLowerCase))
                {
                    environments = await Repository.ListAsync(input.PlanId, input.UserIdSet, environmentNameInLowerCase, logger);
                }
                else
                {
                    environments = await Repository.ListAsync(input.PlanId, input.UserIdSet, logger);
                }
            }

            // Filter out records that they don't have access to
            var environmentIds = CurrentUserProvider.Identity?.AuthorizedEnvironments;
            if (environmentIds != null)
            {
                // The user has a token that limits access to specific environment(s).
                environments = environments.Where((e) => environmentIds.Contains(e.Id));
            }

            return ApplyDeletedFilter(environments, input.EnvironmentListType, logger);
        }

        private IEnumerable<CloudEnvironment> ApplyDeletedFilter(IEnumerable<CloudEnvironment> environments, EnvironmentListType environmentListType, IDiagnosticsLogger logger)
        {
            if (environmentListType == EnvironmentListType.AllEnvironments)
            {
                return environments;
            }

            if (environmentListType == EnvironmentListType.DeletedEnvironments)
            {
                return environments.Where(x => x.IsDeleted == true);
            }
            else
            {
                return environments.Where(x => x.IsDeleted != true);
            }
        }
    }
}
