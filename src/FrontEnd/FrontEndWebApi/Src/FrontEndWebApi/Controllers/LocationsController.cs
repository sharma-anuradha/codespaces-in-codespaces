// <copyright file="LocationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Utility;
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
    [LoggingBaseName("locations_controller")]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    public class LocationsController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationProvider">The current location provider.</param>
        /// <param name="controlPlaneInfo">Control plane information.</param>
        /// <param name="skuCatalog">SKU catalog for the current location.</param>
        /// <param name="currentUserProvider">The current user's profile.</param>
        /// <param name="planManagerSettings">The default plan settings.</param>
        /// <param name="skuUtils">skuUtils to find sku eligiblity.</param>
        public LocationsController(
            ICurrentLocationProvider locationProvider,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            ICurrentUserProvider currentUserProvider,
            PlanManagerSettings planManagerSettings,
            ISkuUtils skuUtils)
        {
            LocationProvider = locationProvider;
            ControlPlaneInfo = controlPlaneInfo;
            SkuCatalog = skuCatalog;
            CurrentUserProvider = currentUserProvider;
            PlanManagerSettings = planManagerSettings;
            SkuUtils = skuUtils;
        }

        private ICurrentLocationProvider LocationProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ISkuCatalog SkuCatalog { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

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
        public IActionResult GetCurrent(
            [FromServices]IDiagnosticsLogger logger)
        {
            var allLocations = ControlPlaneInfo.GetAllDataPlaneLocations().ToArray();
            var result = new LocationsResult
            {
                Current = LocationProvider.CurrentLocation,
                Available = allLocations,
            };

            return Ok(result);
        }

        /// <summary>
        /// Get the list of available control plane stamps.
        /// </summary>
        /// <param name="logger">Target logger.</param>
        /// <returns>JSON-serialized IControlPlaneStampInfo[]</returns>
        [HttpGet("control-planes")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ControlPlaneLocationResult[]), StatusCodes.Status200OK)]
        [HttpOperationalScope("control_planes")]
        public IActionResult GetControlPlaneStamps(
            [FromServices]IDiagnosticsLogger logger)
        {
            var stamps = ControlPlaneInfo.GetControlPlaneStamps();
            var data = new List<ControlPlaneLocationResult>();
            foreach (var stamp in stamps)
            {
                data.Add(new ControlPlaneLocationResult()
                {
                    Location = stamp.Location,
                    DnsHostName = stamp.DnsHostName.ToString(),
                });
            }

            return Ok(data);
        }

        /// <summary>
        /// Gets info about a specific location including what SKUs are available at that location.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="LocationInfoResult"/>.</returns>
        [HttpGet("{location}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LocationInfoResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetAsync(
            [FromRoute]string location,
            [FromServices]IDiagnosticsLogger logger)
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
            var skus = new List<ICloudEnvironmentSku>();
            var skusFilteredByLocation = SkuCatalog.EnabledInternalHardware().Values
                                        .Where((skuObj) => skuObj.SkuLocations.Contains(azureLocation));

            var planInfo = VsoPlanInfo.TryParse(plan);

            foreach (var sku in skusFilteredByLocation)
            {
                var isEnabled = await SkuUtils.IsVisible(sku, planInfo, CurrentUserProvider.Profile);
                if (isEnabled)
                {
                    skus.Add(sku);
                }
            }

            // Clients select default SKUs as the first item in this list.  We control the ordering
            // of the returned SKUs so that clients will show the correct default.  Don't change this
            // unless the clients can handle selecting the default correctly themselves.
            var orderedSkus = skus.OrderBy((sku) => sku.Priority);
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
    }
}
