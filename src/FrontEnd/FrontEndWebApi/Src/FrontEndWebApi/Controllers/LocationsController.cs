// <copyright file="LocationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment locations API controller.
    /// </summary>
    /// <remarks>
    /// Note this controller intentionally does NOT require requests to be authenticated,
    /// because it does not return any sensitive information, only general information about
    /// the availability of services in different locations.
    /// </remarks>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("locations_controller")]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderJwtExtensions.AuthenticationScheme)]
    public class LocationsController : ControllerBase
    {
        private readonly ICurrentLocationProvider locationProvider;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;
        private readonly ICurrentUserProvider currentUserProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationProvider">The current location provider.</param>
        /// <param name="controlPlaneInfo">Control plane information.</param>
        /// <param name="skuCatalog">SKU catalog for the current location.</param>
        /// <param name="currentUserProvider">The current user's profile.</param>
        public LocationsController(
            ICurrentLocationProvider locationProvider,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            ICurrentUserProvider currentUserProvider)
        {
            this.locationProvider = locationProvider;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
            this.currentUserProvider = currentUserProvider;
        }

        /// <summary>
        /// Get the current location and list of globally available locations.
        /// </summary>
        /// <returns>An object result containing the <see cref="LocationsResult"/>.</returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LocationsResult), StatusCodes.Status200OK)]
        public IActionResult GetLocations()
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var allLocations = this.controlPlaneInfo.GetAllDataPlaneLocations().ToArray();
                var result = new LocationsResult
                {
                    Current = locationProvider.CurrentLocation,
                    Available = allLocations,
                };

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetLocations)));

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetLocations)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets info about a specific location including what SKUs are available at that location.
        /// </summary>
        /// <param name="location">The requested location.</param>
        /// <returns>An object result containing the <see cref="LocationInfoResult"/>.</returns>
        [HttpGet("{location}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LocationInfoResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetLocationInfo([FromRoute]string location)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                if (!Enum.TryParse<AzureLocation>(location, ignoreCase: true, out var azureLocation))
                {
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetLocationInfo)));
                    return NotFound();
                }

                IControlPlaneStampInfo owningStamp;
                try
                {
                    owningStamp = this.controlPlaneInfo.GetOwningControlPlaneStamp(azureLocation);
                }
                catch (NotSupportedException)
                {
                    // The requested location is not a known/supported location.
                    return NotFound();
                }

                if (owningStamp.Location != this.locationProvider.CurrentLocation)
                {
                    return RedirectToLocation(owningStamp);
                }

                var profile = currentUserProvider.GetProfile();

                var skus = this.skuCatalog.EnabledInternalHardware().Values
                    .Where((sku) => sku.SkuLocations.Contains(azureLocation))
                    .Where((sku) => ProfileUtils.IsSkuVisibleToProfile(profile, sku));

                /*
                    Clients select default SKUs as the first item in this list.  We control the ordering of the returned SKUs so that clients
                    will show the correct default.  Don't change this unless the clients can handle selecting the default correctly themselves.
                */
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
                };

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetLocationInfo)));

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetLocationInfo)), ex.Message);
                throw;
            }
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
            return skus.OrderBy((sku) => sku.GetActiveVsoUnitsPerHour());
        }
    }
}
