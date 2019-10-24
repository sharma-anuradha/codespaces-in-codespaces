// <copyright file="LocationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
    [AllowAnonymous]
    public class LocationsController : ControllerBase
    {
        private readonly ICurrentLocationProvider locationProvider;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationProvider">The current location provider.</param>
        /// <param name="controlPlaneInfo">Control plane information.</param>
        /// <param name="skuCatalog">SKU catalog for the current location.</param>
        public LocationsController(
            ICurrentLocationProvider locationProvider,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog)
        {
            this.locationProvider = locationProvider;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
        }

        /// <summary>
        /// Get the current location and list of globally available locations.
        /// </summary>
        /// <returns>An object result containing the <see cref="LocationsResult"/>.</returns>
        [HttpGet]
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

                var skus = this.skuCatalog.EnabledInternalHardware().Values
                    .Where((sku) => sku.SkuLocations.Contains(azureLocation))
                    .Select((sku) => new SkuInfoResult
                    {
                        Name = sku.SkuName,
                        DisplayName = sku.DisplayName,
                        OS = sku.ComputeOS.ToString(),
                    })
                    .ToArray();
                var result = new LocationInfoResult
                {
                    Skus = skus,
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
    }
}
