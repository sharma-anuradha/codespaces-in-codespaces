// <copyright file="HealthController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics.Middleware;
using Microsoft.VsSaaS.Diagnostics.Health;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// This controller is used to make sure the service is alive. It should respond to all requests with a 200 OK.
    /// </summary>
    [Route("health")]
    [ApiController]
    public class HealthController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HealthController"/> class.
        /// </summary>
        /// <param name="healthProvider">The health provider.</param>
        public HealthController(
             IHealthProvider healthProvider)
        {
            HealthProvider = healthProvider;
        }

        private IHealthProvider HealthProvider { get; set; }

        /// <summary>
        /// Gets the current health status.
        /// </summary>
        /// <returns>200 if the service is healthy, otherwise 500.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNameSpace)]
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
