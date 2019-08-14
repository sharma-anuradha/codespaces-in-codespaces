// <copyright file="ResourceBrokerController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Abstractions;

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
        /// Allocate a new resource from the resource broker.
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
        public async Task<IActionResult> CreateResourceAsync(
            [FromBody]CreateResourceRequestBody createResourceRequestBody)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (createResourceRequestBody is null)
            {
                logger.AddDuration(duration)
                    .AddReason($"{HttpStatusCode.BadRequest}: body is null")
                    .LogError(GetType().FormatLogErrorMessage(nameof(CreateResourceAsync)));

                return BadRequest();
            }

            try
            {
                var result = await ResourceBrokerHttp.CreateResourceAsync(createResourceRequestBody, logger);

                // Set the location header.
                var locationRelativeUri = ResourceBrokerHttpContract.GetGetResourceUri(result.ResourceIdToken);
                var location = new UriBuilder(Request.GetDisplayUrl())
                {
                    Path = Request.Path = locationRelativeUri,
                }.Uri;

                logger
                    .AddDuration(duration)
                    .AddCreateResourceRequestBody(createResourceRequestBody)
                    .AddResourceBrokerResource(result)
                    .FluentAddValue("LocationHeader", location.AbsoluteUri)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateResourceAsync)));

                // Returns 201-Created with Location header.
                return Created(location, result);
            }
            catch (Exception)
            {
                logger
                    .AddDuration(duration)
                    .AddCreateResourceRequestBody(createResourceRequestBody)
                    .LogError(GetType().FormatLogErrorMessage(nameof(CreateResourceAsync)));
                throw;
            }
        }

        /// <summary>
        /// Gets a resource by id.
        /// <para>
        /// GET api/v1/resourcebroker/resources?id={resourceIdToken}.
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
        /// Deallocates a resource from the resource broker.
        /// <para>
        /// DELETE api/resourcebroker/resources?id={resourceIdToken}.
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

            if (string.IsNullOrEmpty(id))
            {
                logger.AddDuration(duration)
                    .AddReason($"{HttpStatusCode.BadRequest}: id is null or empty")
                    .LogError(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));

                return BadRequest();
            }

            try
            {
                if (!await ResourceBrokerHttp.DeleteResourceAsync(id, logger))
                {
                    logger.AddDuration(duration)
                        .AddResourceIdToken(id)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogInfo(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddResourceIdToken(id)
                    .LogInfo(GetType().FormatLogMessage(nameof(DeallocateAsync)));

                return NoContent();
            }
            catch (Exception)
            {
                logger.AddDuration(duration)
                    .AddResourceIdToken(id)
                    .LogError(GetType().FormatLogErrorMessage(nameof(DeallocateAsync)));
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
        [ProducesResponseType(typeof(StartComputeResponseBody), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartComputeAsync(
            [FromQuery]string id,
            [FromBody]StartComputeRequestBody startComputeRequestBody)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            if (string.IsNullOrEmpty(id))
            {
                logger.AddDuration(duration)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .AddReason($"{HttpStatusCode.BadRequest}: id is null or empty")
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));

                return BadRequest();
            }

            if (startComputeRequestBody is null)
            {
                logger.AddDuration(duration)
                    .AddResourceIdToken(id)
                    .AddReason($"{HttpStatusCode.BadRequest}: body is null")
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));

                return BadRequest();
            }

            try
            {
                var result = await ResourceBrokerHttp.StartComputeAsync(id, startComputeRequestBody, logger);

                logger.AddDuration(duration)
                    .AddResourceIdToken(id)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .LogInfo(GetType().FormatLogMessage(nameof(StartComputeAsync)));

                return Ok(result);
            }
            catch (Exception)
            {
                logger.AddDuration(duration)
                    .AddResourceIdToken(id)
                    .AddStartComputeRequest(startComputeRequestBody)
                    .LogError(GetType().FormatLogErrorMessage(nameof(StartComputeAsync)));
                throw;
            }
        }

        /// <inheritdoc/>
        async Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.CreateResourceAsync(CreateResourceRequestBody requestBody, IDiagnosticsLogger logger)
        {
            // TODO: use IMapper!
            var result = await ResourceBroker.AllocateAsync(
                new ResourceBroker.Models.AllocateInput
                {
                    Location = requestBody.Location.ToString(),
                    SkuName = requestBody.SkuName,
                    Type = (Common.Models.ResourceType)requestBody.Type,
                },
                logger);

            if (!Enum.TryParse<AzureLocation>(result.Location, out var location))
            {
                throw new InvalidOperationException($"Inavlid {nameof(AzureLocation)}: {result.Location}");
            }

            return new ResourceBrokerResource
            {
                Type = requestBody.Type,
                SkuName = result.SkuName,
                ResourceIdToken = result.ResourceId,
                Location = location,
                Created = result.Created,
            };
        }

        /// <inheritdoc/>
        Task<ResourceBrokerResource> IResourceBrokerResourcesHttpContract.GetResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerResourcesHttpContract.DeleteResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            return await ResourceBroker.DeallocateAsync(resourceIdToken, logger);
        }

        /// <inheritdoc/>
        async Task<StartComputeResponseBody> IResourceBrokerResourcesHttpContract.StartComputeAsync(string computeResourceIdToken, StartComputeRequestBody requestBody, IDiagnosticsLogger logger)
        {
            await ResourceBroker.StartComputeAsync(computeResourceIdToken, requestBody.StorageResourceIdToken, requestBody.EnvironmentVariables, logger);
            return new StartComputeResponseBody { };
        }
    }
}
