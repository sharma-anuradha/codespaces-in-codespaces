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
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
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
    [LoggingBaseName("resourcebroker_resources_controller")]
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
        /// <param name="createResourceRequestBody">The allocate request body.</param>
        /// <returns>The <see cref="ResourceBrokerResource"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ResourceBrokerResource), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateResourceSetAsync(
            [FromBody]IEnumerable<CreateResourceRequestBody> createResourcesRequestBody)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (createResourcesRequestBody is null
                || !createResourcesRequestBody.Any())
            {
                logger.AddDuration(duration)
                    .AddReason($"{HttpStatusCode.BadRequest}: body is null")
                    .LogError(GetType().FormatLogErrorMessage(nameof(CreateResourceSetAsync)));

                return BadRequest();
            }

            try
            {
                var result = await ResourceBrokerHttp.CreateResourceSetAsync(createResourcesRequestBody, logger);

                logger
                    .AddDuration(duration)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateResourceSetAsync)));

                // Returns 200-Ok
                return Ok(result);
            }
            catch (Exception e)
            {
                logger
                    .AddDuration(duration)
                    .LogException(GetType().FormatLogErrorMessage(nameof(CreateResourceSetAsync)), e);

                // If we are out of capacity, return 503
                if (e is OutOfCapacityException capacityException)
                {
                    Response.Headers.Add("Retry-After", TimeSpan.FromSeconds(30).TotalSeconds.ToString());
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, e);
                }

                throw;
            }
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <returns>Nothing.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public async Task<IActionResult> GetResourceAsync(
            [FromQuery]string id)
        {
            await Task.CompletedTask;
            _ = id;
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <returns>Nothing.</returns>
        [HttpGet("environmentheartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EnvironmentHeartbeatAsync(
            [FromQuery]string id)
        {
            var logger = HttpContext.GetLogger();

            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid")
                    .LogError(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));

                return BadRequest();
            }

            return await logger.OperationScopeAsync(
                GetType().FormatLogMessage(nameof(EnvironmentHeartbeatAsync)),
                async (childLogger) =>
                {
                    childLogger.FluentAddBaseValue("ResourceId", id);

                    // Get status
                    var result = await ResourceBrokerHttp.TriggerEnvironmentHeartbeatAsync(resourceId, childLogger);

                    // return 200 vs 404 based on the result
                    return result ? (IActionResult)Ok(result) : NotFound(result);
                });
        }

        /// <summary>
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/resourcebroker/resources?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <returns>No content.</returns>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeallocateAsync(
            [FromQuery] string id)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddDuration(duration)
                    .AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid")
                    .LogError(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));

                return BadRequest();
            }

            logger.FluentAddBaseValue("ResourceId", id);

            try
            {
                if (!await ResourceBrokerHttp.DeleteResourceAsync(resourceId, logger))
                {
                    logger.AddDuration(duration)
                        .AddResourceId(resourceId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogInfo(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddResourceId(resourceId)
                    .LogInfo(GetType().FormatLogMessage(nameof(DeallocateAsync)));

                return NoContent();
            }
            catch (Exception)
            {
                logger.AddDuration(duration)
                    .AddResourceId(resourceId)
                    .LogError(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));
                throw;
            }
        }

        /// <summary>
        /// Cleans a resource in the resource broker.
        /// <para>
        /// GET api/resourcebroker/resources/cleanup?id={resourceId}.
        /// </para>
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>No content.</returns>
        [HttpPost("cleanup")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CleanupAsync(
            [FromQuery] string id,
            [FromQuery] string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (!Guid.TryParse(id, out var resourceId))
            {
                logger.AddDuration(duration)
                    .AddReason($"{HttpStatusCode.BadRequest}: id is missing or invalid")
                    .LogError(GetType().FormatLogErrorMessage(nameof(CleanupAsync)));

                return BadRequest();
            }

            try
            {
                if (!await ResourceBrokerHttp.CleanupResourceAsync(resourceId, environmentId, logger))
                {
                    logger.AddDuration(duration)
                        .AddResourceId(resourceId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogInfo(GetType().FormatLogErrorMessage(nameof(CleanupAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddResourceId(resourceId)
                    .LogInfo(GetType().FormatLogMessage(nameof(CleanupAsync)));

                return NoContent();
            }
            catch (Exception)
            {
                logger.AddDuration(duration)
                    .AddResourceId(resourceId)
                    .LogError(GetType().FormatLogErrorMessage(nameof(CleanupAsync)));
                throw;
            }
        }

        /// <summary>
        /// Start a compute resource.
        /// <para>
        /// POST api/v1/resourcebroker/resources/start?id={id}
        /// {<see cref="StartComputeRequestBody"/>}.
        /// </para>
        /// </summary>
        /// <param name="id">The compute resource token id.</param>
        /// <param name="startComputeRequestBody">The start compute request body.</param>
        /// <returns>The <see cref="StartComputeRequestBody"/>.</returns>
        [HttpPost]
        [Route(ResourceBrokerHttpContract.StartComputeOperation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> StartComputeAsync(
            [FromQuery]string id,
            [FromBody]StartComputeRequestBody startComputeRequestBody)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (!Guid.TryParse(id, out var computeResourceId))
            {
                logger.AddDuration(duration)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .AddReason($"{HttpStatusCode.BadRequest}: id missing or invalid")
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));

                return BadRequest();
            }

            logger.FluentAddBaseValue("ResourceId", id);

            if (startComputeRequestBody is null)
            {
                logger.AddDuration(duration)
                    .AddResourceId(computeResourceId)
                    .AddReason($"{HttpStatusCode.BadRequest}: body is null")
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));

                return BadRequest();
            }

            try
            {
                await ResourceBrokerHttp.StartComputeAsync(computeResourceId, startComputeRequestBody, logger);

                logger.AddDuration(duration)
                    .AddResourceId(computeResourceId)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .LogInfo(GetType().FormatLogMessage(nameof(StartComputeAsync)));

                return Ok();
            }
            catch (Exception)
            {
                logger.AddDuration(duration)
                    .AddResourceId(computeResourceId)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));
                throw;
            }
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
                brokerInput, logger.WithValues(new LogValueSet()));

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
        Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.GetResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        Task<bool> IResourceBrokerResourcesHttpContract.TriggerEnvironmentHeartbeatAsync(Guid id, IDiagnosticsLogger logger)
        {
            return ResourceBroker.ExistsAsync(
                id, logger.WithValues(new LogValueSet()));
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.DeleteResourceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            return (await ResourceBroker.DeallocateAsync(new DeallocateInput { ResourceId = resourceId, Trigger = "FrontEndDeleteResourceService" }, logger.WithValues(new LogValueSet()))).Successful;
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.CleanupResourceAsync(Guid resourceId, string environmentId, IDiagnosticsLogger logger)
        {
            return (await ResourceBroker.CleanupAsync(
                new CleanupInput
                {
                    ResourceId = resourceId,
                    Trigger = "FrontEndCleanupResourceService",
                    EnvironmentId = environmentId,
                }, logger.WithValues(new LogValueSet()))).Successful;
        }

        /// <inheritdoc/>
        async Task IResourceBrokerResourcesHttpContract.StartComputeAsync(Guid computeResourceId, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(computeResourceId, nameof(computeResourceId));
            Requires.NotNull(startComputeRequestBody, nameof(startComputeRequestBody));
            Requires.NotEmpty(startComputeRequestBody.StorageResourceId, nameof(startComputeRequestBody.StorageResourceId));
            Requires.NotNullOrEmpty(startComputeRequestBody?.EnvironmentVariables, nameof(startComputeRequestBody.EnvironmentVariables));

            var duration = logger.StartDuration();

            try
            {
                var input = new EnvironmentStartInput
                {
                    ComputeResourceId = computeResourceId,
                    StorageResourceId = startComputeRequestBody.StorageResourceId,
                    EnvironmentVariables = startComputeRequestBody.EnvironmentVariables,
                    Trigger = "FrontEndStartComputeService",
                };

                // Call the resource broker to start the compute.
                var result = await ResourceBroker.StartComputeAsync(input, logger.WithValues(new LogValueSet()));

                logger.AddDuration(duration)
                    .AddResourceId(computeResourceId)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .LogInfo(GetType().FormatLogMessage(nameof(StartComputeAsync)));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .AddResourceId(computeResourceId)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)), ex.Message);
                throw;
            }
        }
    }
}
