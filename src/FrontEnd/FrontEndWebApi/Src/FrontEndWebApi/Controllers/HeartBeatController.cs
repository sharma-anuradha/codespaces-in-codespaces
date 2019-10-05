// <copyright file="HeartBeatController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Monitoring.DataHandlers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Health monitor controller for the front end.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderVMTokenExtensions.AuthenticationScheme)]
    [Route(ServiceConstants.ApiV1Route)]
    [FriendlyExceptionFilter]
    [LoggingBaseName("frontend_heartbeat_controller")]
    public class HeartBeatController : ControllerBase
    {
        private IEnumerable<IDataHandler> handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatController"/> class.
        /// </summary>
        /// <param name="handlers">List of handlers to process monitor states.</param>
        public HeartBeatController(
            IEnumerable<IDataHandler> handlers)
        {
            this.handlers = handlers;
        }

        /// <summary>
        /// Controller to recieve heartbeat messages from VSO Agents.
        /// </summary>
        /// <param name="heartBeat">HeartBeat message.</param>
        /// <returns>TODO: Add documentation.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ProcessHeartBeatAsync([FromBody] HeartBeatBody heartBeat)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            /* TODO: Forward the heartBeat to Backend Service immediatly */

            try
            {
                ValidateResource(heartBeat.ResourceId);
            }
            catch (Exception e)
            {
                logger.AddDuration(duration).
                    LogErrorWithDetail("Invalid VM Resource", e.Message);
                return UnprocessableEntity();
            }

            if (heartBeat.MonitorStates != null && heartBeat.MonitorStates.Length > 0)
            {
                foreach (var entry in heartBeat.MonitorStates)
                {
                    var state = entry.Value;
                    var handler = handlers.Where(h => h.CanProcess(state)).FirstOrDefault();

                    if (handler != default)
                    {
                        try
                        {
                            await handler.ProcessAsync(state, heartBeat.ResourceId, logger);
                        }
                        catch (Exception e)
                        {
                            logger.AddDuration(duration)
                                .LogErrorWithDetail($"Processing failed for the monitor state {state.Name} received from Virtual Machine {heartBeat.ResourceId}", e.Message);
                        }
                    }
                    else
                    {
                        logger.AddDuration(duration)
                            .LogWarning($"No handlers found for processing the monitoring state {state?.Name} received from Virtual Machine {heartBeat.ResourceId}");
                    }
                }
            }

            logger.AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(ProcessHeartBeatAsync)));

            return NoContent();
        }

        private void ValidateResource(string resourceId)
        {
            if (!HttpContext.Items.ContainsKey(AuthenticationBuilderVMTokenExtensions.VMResourceIdName))
            {
                throw new Exception($"Heartbeat VMToken has invalid vmResourceId");
            }

            var vmResourceId = HttpContext.Items[AuthenticationBuilderVMTokenExtensions.VMResourceIdName] as string;
            if (string.IsNullOrWhiteSpace(vmResourceId))
            {
                throw new Exception($"Heartbeat VMToken has invalid vmResourceId");
            }

            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new Exception($"Heartbeat recieved with empty vmResourceId, from the VM {vmResourceId}");
            }

            if (vmResourceId != resourceId)
            {
                throw new Exception($"Heartbeat recieved with conflicting vmResourceId = {resourceId}, from the VM {vmResourceId}");
            }
        }
    }
}
