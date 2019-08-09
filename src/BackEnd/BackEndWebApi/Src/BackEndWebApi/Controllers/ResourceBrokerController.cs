// <copyright file="ResourceBrokerController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
    /// A sample REST API controller.
    /// </summary>
    [ApiController]
    [Route(ResourceBrokerHttpContract.BaseUriPath)]
    public class ResourceBrokerController : ControllerBase, IResourceBrokerHttpContract
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

        private IResourceBrokerHttpContract ResourceBrokerHttp { get => this; }

        /// <summary>
        /// GET api/resourcebroker/resources?id={resourceIdToken}.
        /// </summary>
        /// <param name="id">Resource id token.</param>
        /// <returns>Nothing.</returns>
        [HttpGet]
        [Route(ResourceBrokerHttpContract.ResourcesSubPath)]
        public async Task<IActionResult> GetAsync(
            [FromQuery]string id)
        {
            await Task.CompletedTask;
            _ = id;
            return StatusCode(StatusCodes.Status501NotImplemented);
        }

        /// <summary>
        /// POST api/resourcebroker/resources.
        /// </summary>
        /// <param name="requestBody">The allocate request body.</param>
        /// <returns>The <see cref="AllocateResponseBody"/>.</returns>
        [HttpPost]
        [Route(ResourceBrokerHttpContract.ResourcesSubPath)]
        [ProducesResponseType(typeof(AllocateResponseBody), StatusCodes.Status200OK)]
        public async Task<IActionResult> AllocateAsync([FromBody]AllocateRequestBody requestBody)
        {
            if (requestBody is null)
            {
                return BadRequest();
            }

            var logger = HttpContext.GetLogger();
            try
            {
                var result = await ResourceBrokerHttp.AllocateAsync(requestBody, logger);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogException("TODO: ResourceBrokerController.AllocateAsync", ex);
                throw;
            }
        }

        /// <summary>
        /// DELETE api/resourcebroker/resources?id={resourceIdToken}.
        /// </summary>
        /// <param name="id">The resource id token.</param>
        /// <returns>No content.</returns>
        [HttpDelete]
        [Route(ResourceBrokerHttpContract.ResourcesSubPath)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeallocateAsync(
            [FromQuery] string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            var logger = HttpContext.GetLogger();
            try
            {
                await ResourceBroker.DeallocateAsync(id, logger);
                return NoContent();
            }
            catch (Exception ex)
            {
                logger.LogException("TODO: ResourceBrokerController.DeallocateAsync", ex);
                throw;
            }
        }

        /// <summary>
        /// POST api/resourcebroker/resources/start?id={id}.
        /// </summary>
        /// <param name="id">The compute resource token id.</param>
        /// <param name="startComputeRequestBody">The start compute request body.</param>
        /// <returns>The <see cref="StartComputeRequestBody"/>.</returns>
        [HttpPost]
        [Route(ResourceBrokerHttpContract.StartComputeSubPath)]
        [ProducesResponseType(typeof(StartComputeResponseBody), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartComputeAsync(
            [FromQuery]string id,
            [FromBody]StartComputeRequestBody startComputeRequestBody)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }

            if (startComputeRequestBody is null)
            {
                return BadRequest();
            }

            var logger = HttpContext.GetLogger();
            try
            {
                var result = await ResourceBrokerHttp.StartComputeAsync(id, startComputeRequestBody, logger);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogException("TODO: ResourceBrokerController.StartComputeAsync", ex);
                throw;
            }
        }

        /// <inheritdoc/>
        async Task<AllocateResponseBody> IResourceBrokerHttpContract.AllocateAsync(AllocateRequestBody requestBody, IDiagnosticsLogger logger)
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
                throw new InvalidOperationException("TODO: bad location");
            }

            return new AllocateResponseBody
            {
                Type = requestBody.Type,
                SkuName = result.SkuName,
                ResourceIdToken = result.ResourceId,
                Location = location,
                Created = result.Created,
            };
        }

        /// <inheritdoc/>
        async Task<bool> IResourceBrokerHttpContract.DeallocateAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            return await ResourceBroker.DeallocateAsync(resourceIdToken, logger);
        }

        /// <inheritdoc/>
        async Task<StartComputeResponseBody> IResourceBrokerHttpContract.StartComputeAsync(string computeResourceIdToken, StartComputeRequestBody requestBody, IDiagnosticsLogger logger)
        {
            await ResourceBroker.StartComputeAsync(computeResourceIdToken, requestBody.StorageResourceIdToken, requestBody.EnvironmentVariables, logger);
            return new StartComputeResponseBody
            {
            };
        }
    }
}
