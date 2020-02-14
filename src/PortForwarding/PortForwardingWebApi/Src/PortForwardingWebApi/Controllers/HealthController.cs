// <copyright file="HealthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics.Health;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers
{
    /// <summary>
    /// This controller is used to make sure the service is alive. It should respond to all requests with a 200 OK.
    /// </summary>
    [Route("health")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="healthProvider">The health provider.</param>
        public HealthController(IHealthProvider healthProvider)
        {
            HealthProvider = healthProvider;
        }

        private IHealthProvider HealthProvider { get; set; }

        /// <summary>
        /// Responds to service health probes.
        /// </summary>
        /// <returns>200 OK when service is healthy, 500 Internal Server Error otherwise.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get()
        {
            if (HealthProvider.IsHealthy)
            {
                return Ok();
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}