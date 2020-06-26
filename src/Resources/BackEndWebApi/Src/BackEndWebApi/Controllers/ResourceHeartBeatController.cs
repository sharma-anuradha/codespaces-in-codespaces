// <copyright file="ResourceHeartBeatController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;

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
        /// <param name="resourceHeartBeatManager">Resource HeartBeat Manager.</param>
        /// <param name="mapper">Mapper.</param>
        public ResourceHeartBeatController(IResourceHeartBeatManager resourceHeartBeatManager, IMapper mapper)
        {
            ResourceHeartBeatManager = resourceHeartBeatManager;
            Mapper = mapper;
        }

        private IResourceHeartBeatManager ResourceHeartBeatManager { get; }

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
            [FromRoute]Guid resourceId,
            [FromBody]HeartBeatBody heartBeat,
            [FromServices]IDiagnosticsLogger logger)
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

            try
            {
                await ResourceHeartBeatHttp.UpdateHeartBeatAsync(resourceId, heartBeat, logger.NewChildLogger());
            }
            catch (ResourceNotFoundException)
            {
                return UnprocessableEntity();
            }

            return NoContent();
        }

        /// <inheritdoc/>
        async Task IResourceHeartBeatHttpContract.UpdateHeartBeatAsync(Guid resourceId, HeartBeatBody heartBeat, IDiagnosticsLogger logger)
        {
            logger.AddBaseResourceId(resourceId);

            var heartBeatInput = Mapper.Map<HeartBeatInput>(heartBeat);
            await ResourceHeartBeatManager.SaveHeartBeatAsync(heartBeatInput, logger.NewChildLogger());
        }

        private void ValidateResource(HeartBeatBody heartBeat, Guid resourceId)
        {
            ValidationUtil.IsTrue(resourceId != default, $"Heartbeat received with empty vmResourceId, from the VM");
            ValidationUtil.IsTrue(heartBeat.ResourceId != default, $"Heartbeat received with empty vmResourceId, from the VM {resourceId}");
            ValidationUtil.IsTrue(heartBeat.ResourceId == resourceId, $"Heartbeat received with conflicting vmResourceId = {heartBeat.ResourceId}, from the VM {resourceId}");
        }
    }
}
