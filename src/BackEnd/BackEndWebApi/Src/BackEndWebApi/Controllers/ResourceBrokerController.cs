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
        /// Allocate new resources from the resource broker.
        /// <para>
        /// POST api/v1/resourcebroker/resources.
        /// </para>
        /// <para>
        /// {<see cref="CreateResourceRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="createResourcesRequestBody">The allocate request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="ResourceBrokerResource"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResourceBrokerResource), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("allocate")]
        public async Task<IActionResult> AllocateAsync(
            [FromBody]IEnumerable<CreateResourceRequestBody> createResourcesRequestBody,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (createResourcesRequestBody is null || !createResourcesRequestBody.Any())
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: body is null");
                return BadRequest();
            }

            var result = default(IEnumerable<ResourceBrokerResource>);
            try
            {
                result = await ResourceBrokerHttp.CreateResourceSetAsync(createResourcesRequestBody, logger.NewChildLogger());
            }
            catch (OutOfCapacityException e)
            {
                Response.Headers.Add("Retry-After", TimeSpan.FromSeconds(30).TotalSeconds.ToString());
                return StatusCode(StatusCodes.Status503ServiceUnavailable, e);
            }

            // Returns 200-Ok
            return Ok(result);
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        [HttpOperationalScope("get")]
        public IActionResult GetAsync(
            [FromQuery]string id,
            [FromServices]IDiagnosticsLogger logger)
        {
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Nothing.</returns>
        [HttpGet("environmentheartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("heartbeat")]
        public async Task<IActionResult> ProcessHeartbeatAsync(
            [FromQuery]string id,
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
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>No content.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("heartbeat")]
        public async Task<IActionResult> DeallocateAsync(
            [FromQuery] string id,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(resourceId);

            if (!await ResourceBrokerHttp.DeleteResourceAsync(resourceId, logger.NewChildLogger()))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/resourcebroker/resources/cleanup?id={resourceId}.
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
        public async Task<IActionResult> CleanupAsync(
            [FromQuery] string id,
            [FromQuery] string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid");
                return BadRequest();
            }

            logger.AddBaseResourceId(resourceId);

            if (!await ResourceBrokerHttp.SuspendResourceAsync(resourceId, environmentId, logger.NewChildLogger()))
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Start a compute resource.
        /// <para>
        /// POST api/v1/resourcebroker/resources/start?id={id}
        /// {<see cref="StartResourceRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="id">The compute resource token id.</param>
        /// <param name="startComputeRequestBody">The start compute request body.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>The <see cref="StartResourceRequestBody"/>.</returns>
        [HttpPost]
        [Route(ResourceBrokerHttpContract.StartComputeOperation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("start")]
        public async Task<IActionResult> StartAsync(
            [FromQuery]string id,
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

            await ResourceBrokerHttp.StartResourceSetAsync(computeResourceId, startComputeRequestBody, logger.NewChildLogger());

            return Ok();
        }

        /// <inheritdoc/>
        Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<IEnumerable<ResourceBrokerResource>> IResourceBrokerResourcesHttpContract.CreateResourceSetAsync(
            IEnumerable<CreateResourceRequestBody> createResourcesRequestBody, IDiagnosticsLogger logger)
        {
            var brokerInput = new List<AllocateInput>();
            foreach (var createResourceRequestBody in createResourcesRequestBody)
            {
                brokerInput.Add(new AllocateInput
                    {
                        Location = createResourceRequestBody.Location,
                        SkuName = createResourceRequestBody.SkuName,
                        Type = createResourceRequestBody.Type,
                    });
            }

            var brokerResults = await ResourceBroker.AllocateAsync(
                brokerInput, logger.NewChildLogger());

            var createResourcesResponseBody = new List<ResourceBrokerResource>();
            foreach (var brokerResult in brokerResults)
            {
                createResourcesResponseBody.Add(new ResourceBrokerResource
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
        async Task<bool> IResourceBrokerResourcesHttpContract.DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            var input = new DeleteInput { ResourceId = resourceId, Trigger = "FrontEndDeleteResourceService" };
            var result = await ResourceBroker.DeleteAsync(input, logger.NewChildLogger());
            return result.Successful;
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.SuspendResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger)
        {
            var input = new SuspendInput { ResourceId = resourceId, Trigger = "FrontEndCleanupResourceService", EnvironmentId = environmentId };
            var result = await ResourceBroker.SuspendAsync(input, logger.NewChildLogger());
            return result.Successful;
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.StartResourceSetAsync(Guid computeResourceId, StartResourceRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            var input = new StartInput
            {
                ComputeResourceId = computeResourceId,
                StorageResourceId = startComputeRequestBody.StorageResourceId,
                EnvironmentVariables = startComputeRequestBody.EnvironmentVariables,
                Trigger = "FrontEndStartComputeService",
            };
            var result = await ResourceBroker.StartResourceAsync(input, logger.NewChildLogger());
            return result.Successful;
        }

        /// <inheritdoc/>
        Task<bool> IResourceBrokerResourcesHttpContract.ProcessHeartbeatAsync(Guid id, IDiagnosticsLogger logger)
        {
            return ResourceBroker.ProcessHeartbeatAsync(
                id, logger.NewChildLogger());
        }
    }
}
