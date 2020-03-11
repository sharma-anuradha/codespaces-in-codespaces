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
        private static readonly IComparer<ICloudEnvironmentSku> DisplaySkuComparer = new SkuComparer();

        private readonly ICurrentLocationProvider locationProvider;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;
        private readonly ICurrentUserProvider currentUserProvider;
        private readonly PlanManagerSettings planManagerSettings;
        private readonly ISkuUtils skuUtils;

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
            this.locationProvider = locationProvider;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
            this.currentUserProvider = currentUserProvider;
            this.planManagerSettings = planManagerSettings;
            this.skuUtils = skuUtils;
        }

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
            var allLocations = controlPlaneInfo.GetAllDataPlaneLocations().ToArray();
            var result = new LocationsResult
            {
                Current = locationProvider.CurrentLocation,
                Available = allLocations,
            };

            return Ok(result);
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
                owningStamp = controlPlaneInfo.GetOwningControlPlaneStamp(azureLocation);
            }
            catch (NotSupportedException)
            {
                // The requested location is not a known/supported location.
                return NotFound();
            }

            if (owningStamp.Location != locationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var plan = HttpContext.GetPlan();
            var skus = new List<ICloudEnvironmentSku>();
            var skusFilteredByLocation = skuCatalog.EnabledInternalHardware().Values
                                        .Where((skuObj) => skuObj.SkuLocations.Contains(azureLocation));

            var planInfo = VsoPlanInfo.TryParse(plan);

            foreach (var sku in skusFilteredByLocation)
            {
                var isEnabled = await skuUtils.IsVisible(sku, planInfo, currentUserProvider.GetProfile());
                if (isEnabled)
                {
                    skus.Add(sku);
                }
            }

            // Clients select default SKUs as the first item in this list.  We control the ordering
            // of the returned SKUs so that clients will show the correct default.  Don't change this
            // unless the clients can handle selecting the default correctly themselves.
            var orderedSkus = OrderSkusForDisplay(skus);
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
                DefaultAutoSuspendDelayMinutes = planManagerSettings.DefaultAutoSuspendDelayMinutesOptions,
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

        private IEnumerable<ICloudEnvironmentSku> OrderSkusForDisplay(IEnumerable<ICloudEnvironmentSku> skus)
        {
            return skus.OrderBy((s) => s, DisplaySkuComparer);
        }

        /// <summary>
        /// Compares SKUs for display in clients where the first SKU will be the default for display.
        /// Sort order will place Linux SKUs before Windows, and cheaper SKUs first within an OS.
        /// </summary>
        private class SkuComparer : IComparer<ICloudEnvironmentSku>
        {
            private static readonly Dictionary<ComputeOS, int> OSPriorities = new[]
            {
                ComputeOS.Linux,
                ComputeOS.Windows,
            }.Select((s, i) => new { OS = s, Priority = i, })
                .ToDictionary((x) => x.OS, x => x.Priority);

            public int Compare(ICloudEnvironmentSku x, ICloudEnvironmentSku y)
            {
                if (x.ComputeOS != y.ComputeOS)
                {
                    var xPriority = GetOSPriority(x.ComputeOS);
                    var yPriority = GetOSPriority(y.ComputeOS);

                    return xPriority.CompareTo(yPriority);
                }
                else
                {
                    var xPriority = x.GetActiveVsoUnitsPerHour();
                    var yPriority = y.GetActiveVsoUnitsPerHour();

                    return xPriority.CompareTo(yPriority);
                }
            }

            private static int GetOSPriority(ComputeOS os)
            {
                if (OSPriorities.TryGetValue(os, out var priority))
                {
                    return priority;
                }
                else
                {
                    // Sort unhandled OS at the end, but still differentiated by OS
                    return OSPriorities.Count + (int)os;
                }
            }
        }
    }
}
