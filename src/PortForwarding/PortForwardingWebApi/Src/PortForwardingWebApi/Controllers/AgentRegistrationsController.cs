// <copyright file="AgentRegistrationsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Connections.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Mappings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwardingWebApi.Controllers
{
    /// <summary>
    /// Exposes API for PortForwardingAgent registration management.
    /// </summary>
    [ApiController]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("port_forwarding_agent_registrations")]
    public class AgentRegistrationsController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentRegistrationsController"/> class.
        /// </summary>
        /// <param name="agentMappingClient">Agent mapping client.</param>
        public AgentRegistrationsController(
            IAgentMappingClient agentMappingClient)
        {
            AgentMappingClient = Requires.NotNull(agentMappingClient, nameof(agentMappingClient));
        }

        private IAgentMappingClient AgentMappingClient { get; }

        /// <summary>
        /// Create new agent registration.
        /// </summary>
        /// <param name="registration">The agent registration details.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("post_agent_registration")]
        public async Task<IActionResult> PostAsync(AgentRegistration registration, [FromServices]IDiagnosticsLogger logger)
        {
            await AgentMappingClient.RegisterAgentAsync(registration, logger);

            return Ok();
        }

        /// <summary>
        /// Removes the port forwarding agent from available agents deployment.
        /// </summary>
        /// <param name="agentName">Agent to be removed.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code.</returns>
        [HttpDelete("{agentName}")]
        [HttpDelete("{agentName}/labels")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("remove_busy_agent")]
        public async Task<IActionResult> DeleteLabelsAsync(string agentName, [FromServices] IDiagnosticsLogger logger)
        {
            await AgentMappingClient.RemoveBusyAgentFromDeploymentAsync(agentName, logger);

            return Ok();
        }

        /// <summary>
        /// Removes the port forwarding agent from available agents deployment.
        /// </summary>
        /// <param name="agentName">Agent to be removed.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code.</returns>
        [HttpDelete("{agentName}/pod")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("remove_busy_agent")]
        public async Task<IActionResult> DeleteAgentAsync(string agentName, [FromServices] IDiagnosticsLogger logger)
        {
            await AgentMappingClient.KillAgentAsync(agentName, logger);

            return Ok();
        }

        /// <summary>
        /// Creates the mappings.
        /// </summary>
        /// <param name="mapping">Agent to be mapped.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code.</returns>
        [HttpPost("mappings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("create_mapping")]
        public async Task<IActionResult> CreateMappingAsync(ConnectionDetails mapping, [FromServices] IDiagnosticsLogger logger)
        {
            await AgentMappingClient.CreateAgentConnectionMappingAsync(mapping, logger);

            return Ok();
        }
    }
}
