// <copyright file="PlansController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
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
    [LoggingBaseName("plans_controller")]
    public class PlansController : ControllerBase
    {
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
        /// <returns>An object result containing the <see cref="PlanResult"/>.</returns>
        [HttpGet("{subscriptionId}/{resourceGroupName}/{resourceName}")]
        [ThrottlePerUserHigh(nameof(PlansController), nameof(GetPlansAsync))]
        [ProducesResponseType(typeof(PlanResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPlansAsync(
            [FromRoute]string subscriptionId,
            [FromRoute]string resourceGroupName,
            [FromRoute]string resourceName)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
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
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetPlansAsync)));
                    return NotFound();
                }

                var result = MapAccountToResult(plan, logger);
                if (result == null)
                {
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetPlansAsync)));
                    return NotFound();
                }

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetPlansAsync)));
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetPlansAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Lists all plans belonging to the current user.
        /// </summary>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(PlansController), nameof(ListPlansByOwnerAsync))]
        [ProducesResponseType(typeof(PlanResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListPlansByOwnerAsync()
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                // Match on provider ID instead of profile ID because clients dont have
                // the profile ID when the create the plan resource via ARM.
                // (The provider ID is a combination of "tid" and "oid" claims from the token.)
                var currentUserProviderId = CurrentUserProvider.GetProfile().ProviderId;
                var plans = await PlanManager.ListAsync(
                    currentUserProviderId, subscriptionId: null, resourceGroup: null, logger);

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(ListPlansByOwnerAsync)));
                return Ok(plans.Select((a) => MapAccountToResult(a, logger))
                    .Where((a) => a != null).ToArray());
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ListPlansByOwnerAsync)), ex.Message);
                throw;
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
                logger.LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(MapAccountToResult)), ex.Message);

                // ResourceId is a computed property and may throw.
                // In that case, skip this item and still return the others.
                return null;
            }
        }
    }
}
