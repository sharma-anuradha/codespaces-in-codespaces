// <copyright file="PlansController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderJwtExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName(LoggingBaseName)]
    public class PlansController : ControllerBase
    {
        private const string LoggingBaseName = "plans_controller";

        /// <summary>
        /// Initializes a new instance of the <see cref="PlansController"/> class.
        /// </summary>
        /// <param name="planManager">The front-end plan manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="mapper">The configured auto-mapper.</param>
        public PlansController(
            IPlanManager planManager,
            ICurrentUserProvider currentUserProvider)
        {
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
        }

        private IPlanManager PlanManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <summary>
        /// Get a plan by id.
        /// </summary>
        /// <param name="subscriptionId">The ID of the subscription containing the plan.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the plan.</param>
        /// <param name="resourceName">The name of the plan resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="PlanResult"/>.</returns>
        [HttpGet("{subscriptionId}/{resourceGroupName}/{resourceName}")]
        [ThrottlePerUserHigh(nameof(PlansController), nameof(GetAsync))]
        [ProducesResponseType(typeof(PlanResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetAsync(
            [FromRoute]string subscriptionId,
            [FromRoute]string resourceGroupName,
            [FromRoute]string resourceName,
            [FromServices]IDiagnosticsLogger logger)
        {
            var currentUserId = CurrentUserProvider.GetProfileId();
            var planId = new VsoPlanInfo
            {
                Subscription = subscriptionId,
                ResourceGroup = resourceGroupName,
                Name = resourceName,
            };

            var plan = (await PlanManager.GetAsync(planId, logger)).VsoPlan;
            if (plan == null || plan.UserId != currentUserId)
            {
                return NotFound();
            }

            var result = MapAccountToResult(plan, logger);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        /// <summary>
        /// Lists all plans belonging to the current user.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(PlansController), nameof(ListByOwnerAsync))]
        [ProducesResponseType(typeof(PlanResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [HttpOperationalScope("list_byowner")]
        public async Task<IActionResult> ListByOwnerAsync(
            [FromServices]IDiagnosticsLogger logger)
        {
            var currentUser = CurrentUserProvider.GetProfile();

            // Match on provider ID instead of profile ID because clients dont have
            // the profile ID when the create the plan resource via ARM.
            // (The provider ID is a combination of "tid" and "oid" claims from the token.)
            var currentUserProviderId = currentUser.ProviderId;
            var plans = await PlanManager.ListAsync(
                currentUserProviderId, subscriptionId: null, resourceGroup: null, logger);

            var result = plans.Select((a) => MapAccountToResult(a, logger))
                .Where((a) => a != null).ToArray();

            // If the global limit of plans is exceeded, the client should block new users from creating plans
            var isUserAllowedToCreatePlans =
                result.Length > 0 || await PlanManager.IsPlanCreationAllowedForUserAsync(currentUser, logger);
            if (isUserAllowedToCreatePlans)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private PlanResult MapAccountToResult(VsoPlan vsoPlan, IDiagnosticsLogger logger)
        {
            try
            {
                // Can't use AutoMapper because ResourceId is a computed property.
                var a = vsoPlan.Plan;
                return new PlanResult
                {
                    Id = a.ResourceId,
                    Subscription = a.Subscription,
                    ResourceGroup = a.ResourceGroup,
                    Name = a.Name,
                    Location = a.Location,
                };
            }
            catch (Exception ex)
            {
                logger.LogException($"{LoggingBaseName}_map_account_result", ex);

                // ResourceId is a computed property and may throw.
                // In that case, skip this item and still return the others.
                return null;
            }
        }
    }
}
