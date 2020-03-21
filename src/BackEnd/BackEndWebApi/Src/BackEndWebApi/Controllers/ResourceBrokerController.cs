// <copyright file="ResourceBrokerController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackendWebApi.Controllers
{
    /// <summary>
    /// The resource broker controller for /api/v1/resourcebroker/resources.
    /// </summary>
    [ApiController]
    [Route(ResourceBrokerHttpContract.ResourcesRoute)]
    [LoggingBaseName("backend_resource_broker_controller")]
    public class ResourceBrokerController : ControllerBase, IResourceBrokerResourcesHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceBrokerController"/> class.
        /// </summary>
        /// <param name="resourceBroker">The resource broker.</param>
        public ResourceBrokerController(
            IResourceBroker resourceBroker)
        {
            ResourceBroker = Requires.NotNull(resourceBroker, nameof(resourceBroker));
        }

        private IResourceBroker ResourceBroker { get; }

        private IResourceBrokerResourcesHttpContract ResourceBrokerHttp { get => this; }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={id}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        [HttpOperationalScope("get")]
        public IActionResult GetAsync(
            [Required] Guid id,
            [FromServices] IDiagnosticsLogger logger)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        /// <summary>
        /// Allocate new resources from the resource broker.
        /// <para>
        /// POST api/v1/resourcebroker/resources?environmentId={environmentId}.
        /// </para>
        /// <para>
        /// {<see cref="AllocateRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="resourceRequests">The allocate request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="AllocateResponseBody"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResourceBrokerResource), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("allocate")]
        public async Task<IActionResult> AllocateAsync(
            [Required] Guid environmentId,
            [FromBody, Required, MinLength(1)] IEnumerable<AllocateRequestBody> resourceRequests,
            [FromServices] IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            try
            {
                var result = await ResourceBrokerHttp.AllocateAsync(environmentId, resourceRequests, logger.NewChildLogger());
                return Ok(result);
            }
            catch (OutOfCapacityException e)
            {
                Response.Headers.Add("Retry-After", TimeSpan.FromSeconds(30).TotalSeconds.ToString());
                return StatusCode(StatusCodes.Status503ServiceUnavailable, e);
            }
        }

        /// <summary>
        /// Start a compute resource.
        /// <para>
        /// POST api/v1/resourcebroker/resources/start?environmentId={environmentId}&action=StartCompute
        /// {<see cref="StartResourceRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="action">Target start action.</param>
        /// <param name="resourceRequests">The start request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns><see cref="bool"/> indicating success or not.</returns>
        [HttpPost(ResourceBrokerHttpContract.StartOperation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("start")]
        public async Task<IActionResult> StartAsync(
            [Required] Guid environmentId,
            [Required] StartRequestAction action,
            [FromBody, MinLength(1)] IEnumerable<StartRequestBody> resourceRequests,
            [FromServices] IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            var result = await ResourceBrokerHttp.StartAsync(environmentId, action, resourceRequests, logger.NewChildLogger());
            return Ok(result);
        }

        /// <summary>
        /// suspends a resource in the resource broker.
        /// <para>
        /// GET api/v1/resourcebroker/resources/suspend?environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="resourceRequests">Target suspend request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpPost(ResourceBrokerHttpContract.SuspendOperation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("suspend")]
        public async Task<IActionResult> SuspendAsync(
            [Required] Guid environmentId,
            [FromBody, Required, MinLength(1)] IEnumerable<Guid> resourceRequests,
            [FromServices] IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            var result = await ResourceBrokerHttp.SuspendAsync(environmentId, resourceRequests, logger.NewChildLogger());
            return Ok(result);
        }

        /// <summary>
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/v1/resourcebroker/resources?environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="resourceRequests">Target suspend request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("delete")]
        public async Task<IActionResult> DeleteAsync(
            [Required] Guid environmentId,
            [FromBody, Required, MinLength(1)] IEnumerable<Guid> resourceRequests,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            var result = await ResourceBrokerHttp.DeleteAsync(environmentId, resourceRequests, logger.NewChildLogger());
            return Ok(result);
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/v1/resourcebroker/resources/status?id={resourceId}&environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="id">Target suspend request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpGet(ResourceBrokerHttpContract.StatusOperation)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("status")]
        public async Task<IActionResult> StatusAsync(
            [Required] Guid environmentId,
            [FromQuery, Required, MinLength(1)] IEnumerable<Guid> id,
            [FromServices] IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            var result = await ResourceBrokerHttp.StatusAsync(environmentId, id, logger.NewChildLogger());
            return Ok(result);
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/processheartbeat?id={resourceId}&environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet(ResourceBrokerHttpContract.ProcessHeartbeatOperation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("processheartbeat")]
        public async Task<IActionResult> ProcessHeartbeatAsync(
            [Required] Guid id,
            [Required] Guid environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddBaseEnvironmentId(environmentId);

            var result = await ResourceBrokerHttp.ProcessHeartbeatAsync(environmentId, id, logger.NewChildLogger());
            return Ok(result);
        }

        /// <inheritdoc/>
        Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.GetAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<IEnumerable<AllocateResponseBody>> IResourceBrokerResourcesHttpContract.AllocateAsync(
            Guid environmentId, IEnumerable<AllocateRequestBody> resourceRequests, IDiagnosticsLogger logger)
        {
            var resourceInput = new List<AllocateInput>();
            foreach (var resourceRequest in resourceRequests)
            {
                resourceInput.Add(new AllocateInput
                {
                    Location = resourceRequest.Location,
                    SkuName = resourceRequest.SkuName,
                    Type = resourceRequest.Type,
                });
            }

            var resourceResults = await ResourceBroker.AllocateAsync(
                environmentId, resourceInput, "FrontEndStartComputeService", logger.NewChildLogger());

            var resourceResponses = new List<AllocateResponseBody>();
            foreach (var resourceResult in resourceResults)
            {
                resourceResponses.Add(new AllocateResponseBody
                {
                    Type = resourceResult.Type,
                    SkuName = resourceResult.SkuName,
                    ResourceId = resourceResult.Id,
                    Location = resourceResult.Location,
                    Created = resourceResult.Created,
                });
            }

            return resourceResponses;
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.StartAsync(
            Guid environmentId, StartRequestAction resorceAction, IEnumerable<StartRequestBody> resourceRequests, IDiagnosticsLogger logger)
        {
            var actionInput = (StartAction)((int)resorceAction);
            var resourceInput = new List<StartInput>();
            foreach (var resourceRequest in resourceRequests)
            {
                resourceInput.Add(new StartInput
                {
                    ResourceId = resourceRequest.ResourceId,
                    Variables = resourceRequest.Variables,
                });
            }

            return await ResourceBroker.StartAsync(
                environmentId, actionInput, resourceInput, "FrontEndStartComputeService", logger.NewChildLogger());
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.SuspendAsync(Guid environmentId, IEnumerable<Guid> resourceRequests, IDiagnosticsLogger logger)
        {
            var resourceInput = new List<SuspendInput>();
            foreach (var resourceRequest in resourceRequests)
            {
                resourceInput.Add(new SuspendInput
                {
                    ResourceId = resourceRequest,
                });
            }

            return await ResourceBroker.SuspendAsync(
                environmentId, resourceInput, "FrontEndStartComputeService", logger.NewChildLogger());
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.DeleteAsync(
            Guid environmentId, IEnumerable<Guid> resourceRequests, IDiagnosticsLogger logger)
        {
            var resourceInput = new List<DeleteInput>();
            foreach (var resourceRequest in resourceRequests)
            {
                resourceInput.Add(new DeleteInput
                {
                    ResourceId = resourceRequest,
                });
            }

            return await ResourceBroker.DeleteAsync(
                environmentId, resourceInput, "FrontEndStartComputeService", logger.NewChildLogger());
        }

        /// <inheritdoc/>
        async Task<IEnumerable<StatusResponseBody>> IResourceBrokerResourcesHttpContract.StatusAsync(
            Guid environmentId, IEnumerable<Guid> resourceRequests, IDiagnosticsLogger logger)
        {
            var resourceInput = new List<StatusInput>();
            foreach (var resourceRequest in resourceRequests)
            {
                resourceInput.Add(new StatusInput
                {
                    ResourceId = resourceRequest,
                });
            }

            var resourceResults = await ResourceBroker.StatusAsync(
                environmentId, resourceInput, "FrontEndStartComputeService", logger.NewChildLogger());

            var resourceResponses = new List<StatusResponseBody>();
            foreach (var resourceResult in resourceResults)
            {
                resourceResponses.Add(new StatusResponseBody
                {
                    ResourceId = resourceResult.ResourceId,
                    SkuName = resourceResult.SkuName,
                    Location = resourceResult.Location,
                    Type = resourceResult.Type,
                    ProvisioningStatus = resourceResult.ProvisioningStatus,
                    ProvisioningStatusChanged = resourceResult.ProvisioningStatusChanged,
                    StartingStatus = resourceResult.StartingStatus,
                    StartingStatusChanged = resourceResult.StartingStatusChanged,
                    DeletingStatus = resourceResult.DeletingStatus,
                    DeletingStatusChanged = resourceResult.DeletingStatusChanged,
                    CleanupStatus = resourceResult.CleanupStatus,
                    CleanupStatusChanged = resourceResult.CleanupStatusChanged,
                });
            }

            if (resourceResponses.Count() != resourceRequests.Count())
            {
                throw new InvalidOperationException("Invalid response where result count did not match input count.");
            }

            return resourceResponses;
        }

        /// <inheritdoc/>
        Task<bool> IResourceBrokerResourcesHttpContract.ProcessHeartbeatAsync(
            Guid environmentId, Guid resourceId, IDiagnosticsLogger logger)
        {
            return ResourceBroker.ProcessHeartbeatAsync(
                resourceId, "FrontEndStartComputeService", logger.NewChildLogger());
        }
    }
}
