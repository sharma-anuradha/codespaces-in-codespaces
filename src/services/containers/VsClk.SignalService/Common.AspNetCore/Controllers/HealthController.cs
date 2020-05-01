// <copyright file="HealthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VsCloudKernel.SignalService.Common;

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
            this.logger.Log(healthState ? LogLevel.Debug : LogLevel.Error, $"State:{healthState} => [{string.Join(',', this.healthService.GetProvidersStatus().Select(t => $"({t.Item1.GetFriendlyName()}, {t.Item2})"))}]");

            if (!healthState)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            return Ok();
        }
    }
}
