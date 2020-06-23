// <copyright file="AgentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics.Middleware;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Gets info on the latest Agent.
    /// </summary>
    [ApiController]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("agents_controller")]
    public class AgentsController : Controller
    {
        private readonly AzureLocation azureLocation;
        private readonly IImageUrlGenerator agentUrlGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentsController"/> class.
        /// </summary>
        /// <remarks>
        /// Note this controller intentionally does NOT require requests to be authenticated,
        /// because it does not return any sensitive information, only general read-only information about
        /// the download Uris to the VSO Agent.
        /// </remarks>
        /// <param name="locationProvider">Location provider.</param>
        /// <param name="imageUrlGenerator">Generates the URL to the VM agent.</param>
        public AgentsController(
            ICurrentLocationProvider locationProvider,
            IImageUrlGenerator imageUrlGenerator)
        {
            azureLocation = locationProvider.CurrentLocation;
            agentUrlGenerator = imageUrlGenerator;
        }

        /// <summary>
        /// Retrieves the latest published agent binaries.
        /// </summary>
        /// <param name="family">The agent family, specific to a runtime platform.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        [HttpGet("{family}")]
        [ProducesResponseType(typeof(AgentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get_latest")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNameSpace)]
        public async Task<IActionResult> GetLatestAsync(
            [FromRoute] string family,
            [FromServices]IDiagnosticsLogger logger)
        {
            var (uri, name) = await agentUrlGenerator.ReadOnlyUrlByVMFamily(
                azureLocation, ResourceType.ComputeVM, family, logger.NewChildLogger());

            if (uri == null)
            {
                return NotFound();
            }

            var response = new AgentResponse
            {
                Family = family,
                Name = name,
                AssetUri = uri.ToString(),
            };

            return Ok(response);
        }
    }
}