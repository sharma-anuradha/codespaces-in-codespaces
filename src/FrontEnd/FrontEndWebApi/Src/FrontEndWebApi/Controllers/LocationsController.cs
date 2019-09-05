// <copyright file="LocationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;

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
        private readonly CurrentLocationProvider locationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationsController"/> class.
        /// </summary>
        /// <param name="locationProvider">The current location provider.</param>
        public LocationsController(
            CurrentLocationProvider locationProvider)
        {
            this.locationProvider = locationProvider;
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
                var result = new LocationsResult
                {
                    Current = locationProvider.CurrentLocation,
                    Available = new AzureLocation[]
                    {
                        // TODO: IDataPlaneManager.GetAllDataPlaneLocations()
                        AzureLocation.WestUs2,
                        AzureLocation.WestCentralUs,
                    },
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
        [ProducesResponseType(typeof(LocationsResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<IActionResult> GetLocationInfoAsync([FromRoute]string location)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                if (!Enum.TryParse<AzureLocation>(location, ignoreCase: true, out var azureLocation))
                {
                    // TODO: Also return NotFound if the location is not a supported location.
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetLocationInfoAsync)));
                    return Task.FromResult<IActionResult>(NotFound());
                }

                // TODO: Redirect to the owning control plane for the requested location.
                var result = new LocationInfoResult
                {
                    Skus = new string[]
                    {
                        // TODO: Get list of SKUs available at the location.
                    },
                };

                logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetLocationInfoAsync)));

                return Task.FromResult<IActionResult>(Ok(result));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetLocationInfoAsync)), ex.Message);
                throw;
            }
        }
    }
}
