// <copyright file="HealthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Microsoft.VsCloudKernel.SignalService.Controllers
{
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthService healthService;
        private readonly ILogger logger;

        public HealthController(
            HealthService healthService,
            ILogger<HealthController> logger)
        {
            this.healthService = healthService;
            this.logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetAsync()
        {
            bool healthState = this.healthService.State;
            this.logger.LogDebug($"State:{healthState}");

            if (!healthState)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
