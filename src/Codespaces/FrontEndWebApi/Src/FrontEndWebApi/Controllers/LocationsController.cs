// <copyright file="LocationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics.Middleware;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment locations API controller.
    /// </summary>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    [FriendlyExceptionFilter]
    [LoggingBaseName("locations_controller")]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    public class LocationsController : ControllerBase
    {
        private const string UnauthorizedPlanId = "unauthorized_plan_id";
        private const string UnauthorizedPlanUser = "unauthorized_plan_user";

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationProvider">The current location provider.</param>
        /// <param name="controlPlaneInfo">Control plane information.</param>
        /// <param name="skuCatalog">SKU catalog for the current location.</param>
        /// <param name="currentUserProvider">The current user's profile.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="planManagerSettings">The default plan settings.</param>
        /// <param name="skuUtils">skuUtils to find sku eligiblity.</param>
        public LocationsController(
            ICurrentLocationProvider locationProvider,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            ICurrentUserProvider currentUserProvider,
            IPlanManager planManager,
            PlanManagerSettings planManagerSettings,
            ISkuUtils skuUtils)
        {
            LocationProvider = locationProvider;
            ControlPlaneInfo = controlPlaneInfo;
            SkuCatalog = skuCatalog;
            CurrentUserProvider = currentUserProvider;
            PlanManager = planManager;
            PlanManagerSettings = planManagerSettings;
            SkuUtils = skuUtils;
        }

        private ICurrentLocationProvider LocationProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ISkuCatalog SkuCatalog { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IPlanManager PlanManager { get; }

        private PlanManagerSettings PlanManagerSettings { get; }

        private ISkuUtils SkuUtils { get; }

        /// <summary>
        /// Get the current location and list of globally available locations.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="LocationsResult"/>.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LocationsResult), StatusCodes.Status200OK)]
        [HttpOperationalScope("get_current")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public IActionResult GetCurrent(
            [FromServices] IDiagnosticsLogger logger)
        {
            var allLocations = ControlPlaneInfo.GetAllDataPlaneLocations().ToArray();
            var result = new LocationsResult
            {
                Current = LocationProvider.CurrentLocation,
                Available = allLocations,
            };

            foreach (var location in allLocations)
            {
                var controlPlane = ControlPlaneInfo.GetOwningControlPlaneStamp(location);
                result.Hostnames[location.ToString()] = controlPlane.DnsHostName.ToString();
            }

            return Ok(result);
        }

        /// <summary>
        /// Gets info about a specific location including what SKUs are available at that location.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <param name="planId">The plan ID.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="LocationInfoResult"/>.</returns>
        [HttpGet("{location}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LocationInfoResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("get")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> GetAsync(
            [FromRoute] string location,
            [FromQuery] string planId,
            [FromServices] IDiagnosticsLogger logger)
        {
            if (!Enum.TryParse<AzureLocation>(location, ignoreCase: true, out var azureLocation))
            {
                return NotFound();
            }

            IControlPlaneStampInfo owningStamp;
            try
            {
                owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(azureLocation);
            }
            catch (NotSupportedException)
            {
                // The requested location is not a known/supported location.
                return NotFound();
            }

            if (owningStamp.Location != LocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var plan = CurrentUserProvider.Identity.AuthorizedPlan;

            // We are expecting either plan from auth or planId from query string to be provided from clients.
            // If both plan and planId are provided then we want to make sure that both have the same value.
            if (!string.IsNullOrWhiteSpace(plan) && !string.IsNullOrWhiteSpace(planId) && planId != plan)
            {
                var message = $"{HttpStatusCode.BadRequest}: The planId is not valid: {planId}";
                logger.AddReason(message);
                return BadRequest(message);
            }

            var skus = new List<ICloudEnvironmentSku>();
            var skusFilteredByLocation = SkuCatalog.EnabledInternalHardware().Values
                                        .Where((skuObj) => skuObj.SkuLocations.Contains(azureLocation));

            var planInfo = VsoPlanInfo.TryParse(plan);
            if (planInfo == null && !string.IsNullOrWhiteSpace(planId))
            {
                planInfo = VsoPlanInfo.TryParse(planId);
            }

            var currentUserProfile = await CurrentUserProvider.GetProfileAsync();
            foreach (var sku in skusFilteredByLocation)
            {
                var isEnabled = await SkuUtils.IsVisible(sku, planInfo, currentUserProfile);
                if (isEnabled)
                {
                    skus.Add(sku);
                }
            }

            VsoPlan vsoPlan = null;
            if (planInfo != null)
            {
                vsoPlan = await PlanManager.GetAsync(planInfo, logger);

                if (vsoPlan == null)
                {
                    var message = $"{HttpStatusCode.NotFound}: The plan could not be found: {planInfo.ResourceId}";
                    logger.AddReason(message);
                    return NotFound(message);
                }
                else if (AuthorizePlanAccess(vsoPlan, logger) == false)
                {
                    return new ForbidResult();
                }
            }

            // Clients select default SKUs as the first item in this list.  We control the ordering
            // of the returned SKUs so that clients will show the correct default.  Don't change this
            // unless the clients can handle selecting the default correctly themselves.
            var orderedSkus = skus.OrderBy((sku) => sku.Priority).ToList();

            // Applying the default plan SKU. The default SKU of the plan will be the first one in the
            // list so that clients could show it as default.
            var defaultPlanSku = vsoPlan?.Properties?.DefaultCodespaceSku ?? vsoPlan?.Properties?.DefaultEnvironmentSku;
            if (!string.IsNullOrEmpty(defaultPlanSku))
            {
                var defaultSku = orderedSkus.FirstOrDefault(sku => sku.SkuName == defaultPlanSku);
                if (defaultSku != null)
                {
                    orderedSkus.Remove(defaultSku);
                    orderedSkus.Insert(0, defaultSku);
                }
            }

            var outputSkus = orderedSkus
                .Select((sku) => new SkuInfoResult
                {
                    Name = sku.SkuName,
                    DisplayName = sku.DisplayName,
                    OS = sku.ComputeOS.ToString(),
                })
                .ToArray();

            var result = new LocationInfoResult
            {
                Skus = outputSkus,
                DefaultAutoSuspendDelayMinutes = PlanManagerSettings.DefaultAutoSuspendDelayMinutesOptions,
            };

            return Ok(result);
        }

        private IActionResult RedirectToLocation(IControlPlaneStampInfo owningStamp)
        {
            // Return a 307 to the location-specific hostname for the owning location.
            var builder = new UriBuilder()
            {
                Host = owningStamp.DnsHostName,
                Path = Request.Path,
                Query = Request.QueryString.Value,
                Scheme = Uri.UriSchemeHttps,
            };
            return new RedirectResult(builder.ToString(), permanent: false, preserveMethod: true);
        }

        /// <summary>
        /// Checks if the current user is authorized to access a plan.
        /// </summary>
        /// <param name="plan">Plan that the user is attempting to access.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>True if the user is authorized, else false.</returns>
        private bool AuthorizePlanAccess(
            VsoPlan plan,
            IDiagnosticsLogger logger)
        {
            var isPlanAuthorized = CurrentUserProvider.Identity.IsPlanAuthorized(plan.Plan.ResourceId);
            if (isPlanAuthorized == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                logger.LogWarning(UnauthorizedPlanId);
                return false;
            }

            if (plan.UserId != null)
            {
                // Users without a scoped access token must be the owner of the plan
                // (if the plan has an owner).
                var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;
                if (!currentUserIdSet.EqualsAny(plan.UserId))
                {
                    logger.LogWarning(UnauthorizedPlanUser);
                    return false;
                }
            }
            else if (isPlanAuthorized != true)
            {
                // Users must have explicit authorization for unowned plans.
                logger.LogWarning(UnauthorizedPlanId);
                return false;
            }

            return true;
        }
    }
}
