// <copyright file="ResourceHeartBeatController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Controllers
{
    /// <summary>
    /// Heartbeat controller for the back end.
    /// </summary>
    [ApiController]
    [Route(ResourceHeartBeatHttpContract.HeartBeatV1Route)]
    [LoggingBaseName("backend_heartbeat_controller")]
    public class ResourceHeartBeatController : ControllerBase, IResourceHeartBeatHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceHeartBeatController"/> class.
        /// </summary>
        /// <param name="resourceContinuation">Resource continuation.</param>
        /// <param name="mapper">Mapper.</param>
        public ResourceHeartBeatController(IResourceContinuationOperations resourceContinuation, IMapper mapper)
        {
            ResourceContinuation = resourceContinuation;
            Mapper = mapper;
        }

        private IResourceContinuationOperations ResourceContinuation { get; }

        private IResourceHeartBeatHttpContract ResourceHeartBeatHttp { get => this; }

        private IMapper Mapper { get; }

        /// <summary>
        /// Controller to recieve heartbeat messages from VSO Agents.
        /// </summary>
        /// <param name="resourceId">VM Resource Id.</param>
        /// <param name="heartBeat">HeartBeat message.</param>
        /// <param name="logger">ILogger object.</param>
        /// <returns>The IActionResult.</returns>
        [HttpPost("{resourceId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [HttpOperationalScope("update")]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute] Guid resourceId,
            [FromBody] HeartBeatBody heartBeat,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                ValidateResource(heartBeat, resourceId);
            }
            catch (Exception e)
            {
                logger.AddReason(e.Message);
                return UnprocessableEntity();
            }

            var loggingProperties = new Dictionary<string, string>();

            if (Request.Headers.TryGetValue(HttpConstants.CorrelationIdHeader, out var correlationId))
            {
                loggingProperties.Add(ResourceLoggingConstants.RequestCorrelationId, correlationId.First());
            }

            await ResourceHeartBeatHttp.UpdateHeartBeatAsync(resourceId, heartBeat, logger.NewChildLogger(), loggingProperties);

            return NoContent();
        }

        /// <inheritdoc/>
        async Task IResourceHeartBeatHttpContract.UpdateHeartBeatAsync(Guid resourceId, HeartBeatBody heartBeat, IDiagnosticsLogger logger, IDictionary<string, string> loggingProperties)
        {
            logger.AddBaseResourceId(resourceId);

            var heartBeatInput = Mapper.Map<HeartBeatInput>(heartBeat);
            if (heartBeatInput == null)
            {
                throw new ArgumentNullException(nameof(heartBeatInput));
            }

            await ResourceContinuation.ProcessHeartbeatAsync(heartBeatInput, logger.NewChildLogger(), loggingProperties);
        }

        private void ValidateResource(HeartBeatBody heartBeat, Guid resourceId)
        {
            ValidationUtil.IsTrue(resourceId != default, $"Heartbeat received with empty vmResourceId, from the VM");
            ValidationUtil.IsTrue(heartBeat.ResourceId != default, $"Heartbeat received with empty vmResourceId, from the VM {resourceId}");
            ValidationUtil.IsTrue(heartBeat.ResourceId == resourceId, $"Heartbeat received with conflicting vmResourceId = {heartBeat.ResourceId}, from the VM {resourceId}");
        }
    }
}
