// <copyright file="EnvironmentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.EnvironmentsV1Route)]
    [LoggingBaseName("environments_controller")]
    public class EnvironmentsController : ControllerBase /* TODO add this later IEnvironmentsHttpContract */
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
        /// <param name="environmentId">The environment id.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet("{environmentId}")]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnvironmentAsync(
            [FromRoute]string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                ValidationUtil.IsRequired(environmentId, nameof(environmentId));

                var currentUserId = CurrentUserProvider.GetProfileId();
                var result = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
                if (result is null)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(GetEnvironmentAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(result)
                    .LogInfo(GetType().FormatLogMessage(nameof(GetEnvironmentAsync)));

                return Ok(Mapper.Map<CloudEnvironmentResult>(result));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddEnvironmentId(environmentId)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(GetEnvironmentAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Lists all cloud environments belonging to the the current user.
        /// </summary>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CloudEnvironmentResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ListEnvironmentsByOwnerAsync()
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var currentUserId = CurrentUserProvider.GetProfileId();

                var modelsRaw = await EnvironmentManager.GetEnvironmentsByOwnerAsync(currentUserId, logger);
                if (modelsRaw is null)
                {
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.NotFound}: no environments for current user.")
                        .LogError(GetType().FormatLogErrorMessage(nameof(ListEnvironmentsByOwnerAsync)));

                    // TODO: why not return 200 with empty collection?
                    return NotFound();
                }

                logger.AddDuration(duration)
                    .FluentAddValue("Count", modelsRaw.Count().ToString())
                    .LogInfo(GetType().FormatLogMessage(nameof(ListEnvironmentsByOwnerAsync)));

                return Ok(Mapper.Map<CloudEnvironmentResult[]>(modelsRaw));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ListEnvironmentsByOwnerAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create a new cloud environment.
        /// </summary>
        /// <param name="createEnvironmentInput">The cloud environment input.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCloudEnvironmentAsync(
            [FromBody]CreateCloudEnvironmentBody createEnvironmentInput)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                ValidationUtil.IsRequired(createEnvironmentInput, nameof(createEnvironmentInput));
                ValidationUtil.IsRequired(createEnvironmentInput.FriendlyName, nameof(createEnvironmentInput.FriendlyName));
                ValidationUtil.IsRequired(createEnvironmentInput.Type, nameof(createEnvironmentInput.Type));

                var cloudEnvironment = Mapper.Map<CreateCloudEnvironmentBody, CloudEnvironment>(createEnvironmentInput);

                // TODO HACK: specify a temporary sku. Old clients don't specify one.
                if (string.IsNullOrEmpty(cloudEnvironment.SkuName))
                {
                    cloudEnvironment.SkuName = "Small-Linux-Preview";
                }

                // TODO HACK: specify a temporary location. Old clients don't specify one.
                if (cloudEnvironment.Location == default)
                {
                    cloudEnvironment.Location = AzureLocation.EastUs;
                }

                var currentUserId = CurrentUserProvider.GetProfileId();
                var accessToken = CurrentUserProvider.GetBearerToken();

                cloudEnvironment = await EnvironmentManager.CreateEnvironmentAsync(
                    cloudEnvironment,
                    new CloudEnvironmentOptions { CreateFileShare = createEnvironmentInput.CreateFileShare },
                    currentUserId,
                    accessToken,
                    logger);

                if (cloudEnvironment is null)
                {
                    // Couldn't be registered. Assume it already exists?
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.Conflict}: already exists?")
                        .LogError(GetType().FormatLogErrorMessage(nameof(CreateCloudEnvironmentAsync)));

                    // TODO: 409 conflict might mean that the requested session id already exists
                    // Could mean that the friendly-name is in conflict, could mean anything!
                    return StatusCode(StatusCodes.Status409Conflict);
                }

                var location = new UriBuilder(Request.GetDisplayUrl());
                location.Path = location.Path.TrimEnd('/');
                location.Path = $"{location.Path}/{cloudEnvironment.Id}";

                logger.AddDuration(duration)
                    .AddCloudEnvironment(cloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateCloudEnvironmentAsync)));

                return Created(location.Uri, Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(cloudEnvironment));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateCloudEnvironmentAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Delete a cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>Status code <see cref="HttpStatusCode.NoContent"/> if deleted, otherwise <see cref="HttpStatusCode.NotFound"/>.</returns>
        [HttpDelete("{environmentId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCloudEnvironmentAsync(
            [FromRoute]string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                ValidationUtil.IsRequired(environmentId, nameof(environmentId));

                var currentUserId = CurrentUserProvider.GetProfileId();

                var result = await EnvironmentManager.DeleteEnvironmentAsync(
                    environmentId,
                    currentUserId,
                    logger);

                if (!result)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogWarning(GetType().FormatLogMessage(nameof(DeleteCloudEnvironmentAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddEnvironmentId(environmentId)
                    .LogInfo(GetType().FormatLogMessage(nameof(DeleteCloudEnvironmentAsync)));

                return NoContent();
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddEnvironmentId(environmentId)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(DeleteCloudEnvironmentAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Register a callback for an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="callbackBody">The callback info.</param>
        /// <returns>An object result that containts the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost("{environmentId}/_callback")] // TODO: This should be PATCH not POST
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEnvironmentCallbackAsync(
            [FromRoute]string environmentId,
            [FromBody]CallbackOptionsBody callbackBody)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                ValidationUtil.IsRequired(environmentId, nameof(environmentId));
                ValidationUtil.IsRequired(callbackBody, nameof(callbackBody));
                ValidationUtil.IsRequired(callbackBody?.Payload, nameof(callbackBody.Payload));
                ValidationUtil.IsRequired(callbackBody?.Payload?.SessionId, nameof(callbackBody.Payload.SessionId));
                ValidationUtil.IsRequired(callbackBody?.Payload?.SessionPath, nameof(callbackBody.Payload.SessionPath));

                var currentUserId = CurrentUserProvider.GetProfileId();
                var options = Mapper.Map<CallbackOptionsBody, CallbackOptions>(callbackBody);
                var result = await EnvironmentManager.UpdateEnvironmentCallbackAsync(
                    environmentId,
                    options,
                    currentUserId,
                    logger);

                if (result is null)
                {
                    logger
                        .AddEnvironmentId(environmentId)
                        .AddSessionId(callbackBody.Payload.SessionId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(UpdateEnvironmentCallbackAsync)));

                    return NotFound();
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(result)
                    .LogInfo(GetType().FormatLogMessage(nameof(UpdateEnvironmentCallbackAsync)));

                return Ok(Mapper.Map<CloudEnvironmentResult>(result));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(UpdateEnvironmentCallbackAsync)), ex.Message);
                throw;
            }
        }
    }
}
