// <copyright file="ResourceBrokerController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            string id,
            [FromServices]IDiagnosticsLogger logger)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        /// <summary>
        /// Allocate new resources from the resource broker.
        /// <para>
        /// POST api/v1/resourcebroker/resources.
        /// </para>
        /// <para>
        /// {<see cref="AllocateRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="allocateRequestBody">The allocate request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="ResourceBrokerResource"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResourceBrokerResource), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("allocate")]
        public async Task<IActionResult> AllocateAsync(
            [FromBody]IEnumerable<AllocateRequestBody> allocateRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (allocateRequestBody is null || !allocateRequestBody.Any())
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: body is null");
                return BadRequest();
            }

            try
            {
                var result = await ResourceBrokerHttp.AllocateAsync(allocateRequestBody, logger.NewChildLogger());
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
        /// POST api/v1/resourcebroker/resources/obsoletestart?id={id}
        /// {<see cref="StartResourceRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="id">The compute resource token id.</param>
        /// <param name="startComputeRequestBody">The start compute request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="StartResourceRequestBody"/>.</returns>
        [HttpPost("obsoletestart")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("obsoletestart")]
        [Obsolete]
        public async Task<IActionResult> ObsoleteStartAsync(
            string id,
            [FromBody]StartResourceRequestBody startComputeRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var computeResourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(computeResourceId)
                .AddStartResourceRequest(startComputeRequestBody);

            if (startComputeRequestBody is null)
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: body is null");
                return BadRequest();
            }

            if (!startComputeRequestBody.EnvironmentVariables.Any())
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: environment variables required");
                return BadRequest();
            }

            await ResourceBrokerHttp.StartAsync(computeResourceId, startComputeRequestBody, logger.NewChildLogger());

            return Ok();
        }

        /// <summary>
        /// Start a compute resource.
        /// <para>
        /// POST api/v1/resourcebroker/resources/?id={id}
        /// {<see cref="StartResourceRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="id">The compute resource token id.</param>
        /// <param name="startComputeRequestBody">The start compute request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="StartResourceRequestBody"/>.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("start")]
        public Task<IActionResult> StartAsync(
            string id,
            [FromBody]StartResourceRequestBody startComputeRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            return ObsoleteStartAsync(id, startComputeRequestBody, logger);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/v1/resourcebroker/resources/cleanup?id={id}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpPost("cleanup")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("cleanup")]
        [Obsolete]
        public async Task<IActionResult> ObsoleteCleanupAsync(
            string id,
            string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var typedResourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: resourceId is missing or invalid");
                return BadRequest();
            }

            if (!Guid.TryParse(environmentId, out var typedEnvironmentId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: environmentId is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(typedResourceId)
                .AddBaseEnvironmentId(typedEnvironmentId);

            if (!await ResourceBrokerHttp.SuspendAsync(typedResourceId, typedEnvironmentId, logger.NewChildLogger()))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/v1/resourcebroker/resources/obsoletesuspend?environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="suspendRequestBody">Target suspend request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpPost("obsoletesuspend")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("obsoletesuspend")]
        [Obsolete]
        public async Task<IActionResult> ObsoleteSuspendAsync(
            string environmentId,
            [FromBody]IEnumerable<SuspendRequestBody> suspendRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (suspendRequestBody is null || !suspendRequestBody.Any())
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: body is null");
                return BadRequest();
            }

            if (!Guid.TryParse(environmentId, out var typedEnvironmentId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: environmentId is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseEnvironmentId(typedEnvironmentId);

            if (!await ResourceBrokerHttp.SuspendAsync(suspendRequestBody, typedEnvironmentId, logger.NewChildLogger()))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/v1/resourcebroker/resources/suspend?environmentId={environmentId}.
        /// </para>
        /// </summary>
        /// <param name="environmentId">Target environment id.</param>
        /// <param name="suspendRequestBody">Target suspend request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpPost("suspend")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("suspend")]
        public Task<IActionResult> SuspendAsync(
            string environmentId,
            [FromBody]IEnumerable<SuspendRequestBody> suspendRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            return ObsoleteSuspendAsync(environmentId, suspendRequestBody, logger);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/v1/resourcebroker/obsoletedelete?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpDelete("obsoletedelete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("obsoletedelete")]
        [Obsolete]
        public async Task<IActionResult> ObsoleteDeleteAsync(
            string id,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(resourceId);

            if (!await ResourceBrokerHttp.DeleteAsync(resourceId, logger.NewChildLogger()))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/v1/resourcebroker/resources?id={id}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("delete")]
        public Task<IActionResult> DeleteAsync(
            string id,
            [FromServices]IDiagnosticsLogger logger)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            return ObsoleteDeleteAsync(id, logger);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/obsoleteenvironmentheartbeat?id={id}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet("obsoleteenvironmentheartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("obsoleteheartbeat")]
        [Obsolete]
        public async Task<IActionResult> ObsoleteProcessHeartbeatAsync(
            string id,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(resourceId);

            // Get status
            var result = await ResourceBrokerHttp.ProcessHeartbeatAsync(resourceId, logger.NewChildLogger());

            // return 200 vs 404 based on the result
            return result ? (IActionResult)Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/environmentheartbeat?id={id}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet("environmentheartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("heartbeat")]
        public Task<IActionResult> ProcessHeartbeatAsync(
            string id,
            [FromServices]IDiagnosticsLogger logger)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            return ObsoleteProcessHeartbeatAsync(id, logger);
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <inheritdoc/>
        Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.GetAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<IEnumerable<AllocateResponseBody>> IResourceBrokerResourcesHttpContract.AllocateAsync(
            IEnumerable<AllocateRequestBody> allocateRequestBody, IDiagnosticsLogger logger)
        {
            var brokerInput = new List<AllocateInput>();
            foreach (var body in allocateRequestBody)
            {
                brokerInput.Add(new AllocateInput
                    {
                        Location = body.Location,
                        SkuName = body.SkuName,
                        Type = body.Type,
                    });
            }

            var brokerResults = await ResourceBroker.AllocateAsync(
                brokerInput, logger.NewChildLogger());

            var createResourcesResponseBody = new List<AllocateResponseBody>();
            foreach (var brokerResult in brokerResults)
            {
                createResourcesResponseBody.Add(new AllocateResponseBody
                    {
                        Type = brokerResult.Type,
                        SkuName = brokerResult.SkuName,
                        ResourceId = brokerResult.Id,
                        Location = brokerResult.Location,
                        Created = brokerResult.Created,
                    });
            }

            return createResourcesResponseBody;
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.StartAsync(Guid computeResourceId, StartResourceRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            var input = new StartInput
            {
                ComputeResourceId = computeResourceId,
                StorageResourceId = startComputeRequestBody.StorageResourceId,
                EnvironmentVariables = startComputeRequestBody.EnvironmentVariables,
                Trigger = "FrontEndStartComputeService",
            };
            return await ResourceBroker.StartAsync(input, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        [Obsolete]
        async Task<bool> IResourceBrokerResourcesHttpContract.SuspendAsync(Guid resourceId, Guid environmentId, IDiagnosticsLogger logger)
        {
            var input = new SuspendInput
                { ResourceId = resourceId, Trigger = "FrontEndSuspendResourceService", EnvironmentId = environmentId };
            return await ResourceBroker.SuspendAsync(
                input, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.SuspendAsync(
            IEnumerable<SuspendRequestBody> suspendRequestBody, Guid environmentId, IDiagnosticsLogger logger)
        {
            var brokerInput = new List<SuspendInput>();
            foreach (var body in suspendRequestBody)
            {
                brokerInput.Add(new SuspendInput
                {
                    ResourceId = body.ResourceId,
                    Trigger = "FrontEndSuspendResourceService",
                    EnvironmentId = environmentId,
                });
            }

            return await ResourceBroker.SuspendAsync(
                brokerInput, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.DeleteAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            var input = new DeleteInput { ResourceId = resourceId, Trigger = "FrontEndDeleteResourceService" };
            return await ResourceBroker.DeleteAsync(input, logger.NewChildLogger());
        }

        /// <inheritdoc/>
        Task<bool> IResourceBrokerResourcesHttpContract.ProcessHeartbeatAsync(Guid id, IDiagnosticsLogger logger)
        {
            return ResourceBroker.ProcessHeartbeatAsync(
                id, logger.NewChildLogger());
        }
    }
}
