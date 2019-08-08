// <copyright file="EnvironmentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The environment registration API controller.
    /// </summary>
    [ApiController]
    [Authorize]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.EnvironmentsV1Route)]
    public class EnvironmentsController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentsController"/> class.
        /// </summary>
        /// <param name="environmentManager">The front-end environment manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="mapper">The configured auto-mapper.</param>
        public EnvironmentsController(
            ICloudEnvironmentManager environmentManager,
            ICurrentUserProvider currentUserProvider,
            IMapper mapper)
        {
            Requires.NotNull(environmentManager, nameof(environmentManager));
            Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            Requires.NotNull(mapper, nameof(mapper));

            EnvironmentManager = environmentManager;
            CurrentUserProvider = currentUserProvider;
            Mapper = mapper;
        }

        private ICloudEnvironmentManager EnvironmentManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IMapper Mapper { get; }

        /// <summary>
        /// Get an environment by id.
        /// </summary>
        /// <example>
        /// GET environments/{id}
        /// GET api/environment/registration/{id}
        /// .
        /// </example>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet("{environmentId}")]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnvironment(
            [FromRoute]string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var result = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
            if (result == null)
            {
                return NotFound();
            }

            logger.AddRegistrationInfoToResponseLog(result);
            return Ok(Mapper.Map<CloudEnvironmentResult>(result));
        }

        /// <summary>
        /// Gets all environment registrations belonging to the the current user.
        /// </summary>
        /// <example>
        /// GET environments
        /// GET api/environment/registration
        /// .
        /// </example>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CloudEnvironmentResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnvironmentsByOwnerAsync()
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var modelsRaw = await EnvironmentManager.GetEnvironmentsByOwnerAsync(currentUserId, logger);
            if (modelsRaw == null)
            {
                return NotFound();
            }

            return Ok(Mapper.Map<CloudEnvironmentResult[]>(modelsRaw));
        }

        /// <summary>
        /// Create a new environment registration.
        /// </summary>
        /// <example>
        /// POST environments
        /// POST api/environment/registration
        /// .
        /// </example>
        /// <param name="createEnvironmentInput">The environment registration input.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create(
            [FromBody]CloudEnvironmentInput createEnvironmentInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();
            var accessToken = CurrentUserProvider.GetBearerToken();

            ValidationUtil.IsRequired(createEnvironmentInput, nameof(createEnvironmentInput));
            ValidationUtil.IsRequired(createEnvironmentInput.FriendlyName, nameof(createEnvironmentInput.FriendlyName));
            ValidationUtil.IsRequired(createEnvironmentInput.Type, nameof(createEnvironmentInput.Type));

            var model = Mapper.Map<CloudEnvironmentInput, CloudEnvironment>(createEnvironmentInput);

            model = await EnvironmentManager.CreateEnvironment(
                model,
                new CloudEnvironmentOptions { CreateFileShare = createEnvironmentInput.CreateFileShare },
                currentUserId,
                accessToken,
                logger);

            if (model != null)
            {
                logger.AddRegistrationInfoToResponseLog(model);
                return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(model));
            }

            // Couldn't be registered. Assume it already exists?
            return StatusCode((int)HttpStatusCode.Conflict);
        }

        /// <summary>
        /// Replace an existing environment.
        /// </summary>
        /// <example>
        /// PUT environments/{id}
        /// PUT api/environment/registration/{id}
        /// .
        /// </example>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>Error status code <see cref="HttpStatusCode.NotImplemented"/>.</returns>
        [HttpPut("{environmentId}")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public Task<IActionResult> Put(
            [FromRoute]string environmentId)
        {
            _ = environmentId;
            return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
        }

        /// <summary>
        /// Delete an environment registration.
        /// </summary>
        /// <example>
        /// DELETE environments/{id}
        /// DELETE api/environment/registration/{id}
        /// .
        /// </example>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>Status code <see cref="HttpStatusCode.NoContent"/> if deleted, otherwise <see cref="HttpStatusCode.NotFound"/>.</returns>
        [HttpDelete("{environmentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            [FromRoute]string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            logger.AddEnvironmentId(environmentId);

            var result = await EnvironmentManager.DeleteEnvironmentAsync(
                environmentId,
                currentUserId,
                logger);

            if (!result)
            {
                logger.LogWarning("env_not_found");
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Update an existing environment registration.
        /// </summary>
        /// <example>
        /// PATCH environments/{id}
        /// PATCH api/environment/registration/{id}
        /// .
        /// </example>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>Error status code <see cref="HttpStatusCode.NotImplemented"/>.</returns>
        [HttpPatch("{environmentId}")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public Task<IActionResult> Patch(
            [FromRoute]string environmentId)
        {
            _ = environmentId;
            return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
        }

        /// <summary>
        /// Get a task associated with an environment.
        /// </summary>
        /// <param name="id">The environment id.</param>
        /// <param name="taskId">The task id.</param>
        /// <returns>Error status code <see cref="HttpStatusCode.NotImplemented"/>.</returns>
        // GET api/environment/registration/<id>/tasks/<taskId>
        [HttpGet("{id}/tasks/{taskId}")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public Task<IActionResult> GetTask(
            [FromRoute]string id,
            [FromRoute]string taskId)
        {
            return Task.FromResult<IActionResult>(StatusCode((int)HttpStatusCode.NotImplemented));
        }

        /// <summary>
        /// Register a callback for an environment.
        /// </summary>
        /// <example>
        /// POST  api/environment/registration/{id}/_callback
        /// .
        /// </example>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="updateCloudEnvironmentCallbackInput">The callback info.</param>
        /// <returns>An object result that containts the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost("{environmentId}/_callback")]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEnvironmentCallbackAsync(
            [FromRoute]string environmentId,
            [FromBody]CallbackInput updateCloudEnvironmentCallbackInput)
        {
            var logger = HttpContext.GetLogger();
            var currentUserId = CurrentUserProvider.GetProfileId();

            var options = Mapper.Map<CallbackInput, CallbackOptions>(updateCloudEnvironmentCallbackInput);

            var result = await EnvironmentManager.UpdateEnvironmentCallbackAsync(
                environmentId,
                options,
                currentUserId,
                logger);

            if (result == null)
            {
                logger
                    .AddEnvironmentId(environmentId)
                    .AddSessionId(updateCloudEnvironmentCallbackInput.Payload.SessionId)
                    .LogError("env_not_found_on_callback");
                return NotFound();
            }

            logger.AddRegistrationInfoToResponseLog(result);
            return Ok(Mapper.Map<CloudEnvironmentResult>(result));
        }
    }
}
