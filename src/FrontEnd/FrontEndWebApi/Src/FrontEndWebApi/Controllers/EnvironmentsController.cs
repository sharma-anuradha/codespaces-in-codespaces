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
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Duration = Microsoft.VsSaaS.Diagnostics.Extensions.DiagnosticsLoggerExtensions.Duration;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthenticationBuilderJwtExtensions.AuthenticationScheme)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("environments_controller")]
    public class EnvironmentsController : ControllerBase /* TODO add this later IEnvironmentsHttpContract */
    {
        private const string StartEnvironmentHttpVerb = "start";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentsController"/> class.
        /// </summary>
        /// <param name="environmentManager">The front-end environment manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="currentLocationProvider">The current location provider.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="mapper">The configured auto-mapper.</param>
        /// <param name="serviceUriBuilder">The service uri builder.</param>
        public EnvironmentsController(
            ICloudEnvironmentManager environmentManager,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider,
            ISkuCatalog skuCatalog,
            IMapper mapper,
            IServiceUriBuilder serviceUriBuilder)
        {
            EnvironmentManager = Requires.NotNull(environmentManager, nameof(environmentManager));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
            ServiceUriBuilder = Requires.NotNull(serviceUriBuilder, nameof(serviceUriBuilder));
        }

        private ICloudEnvironmentManager EnvironmentManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IMapper Mapper { get; }

        private IServiceUriBuilder ServiceUriBuilder { get; }

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
        /// Lists all cloud environments belonging to the the current user and environment name (optional)
        /// </summary>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(CloudEnvironmentResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListEnvironmentsAsync(
            [FromQuery]string name, [FromQuery]string planId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var currentUserId = CurrentUserProvider.GetProfileId();

                var modelsRaw = await EnvironmentManager.ListEnvironmentsAsync(currentUserId, name, planId, logger);

                logger.AddDuration(duration)
                    .FluentAddValue("Count", modelsRaw.Count().ToString())
                    .LogInfo(GetType().FormatLogMessage(nameof(ListEnvironmentsAsync)));

                return Ok(Mapper.Map<CloudEnvironmentResult[]>(modelsRaw));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ListEnvironmentsAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Shutdown a cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/shutdown")]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ShutdownCloudEnvironmentAsync([FromRoute] string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                ValidationUtil.IsRequired(environmentId, nameof(environmentId));

                var currentUserId = CurrentUserProvider.GetProfileId();

                var environment = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
                if (environment == null)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogWarning(GetType().FormatLogMessage(nameof(ShutdownCloudEnvironmentAsync)));

                    return NotFound();
                }

                // Reroute to correct location if needed
                var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
                if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
                {
                    return RedirectToLocation(owningStamp);
                }

                // We are in the right location, go ahead and shutdown
                var result = await EnvironmentManager.ShutdownEnvironmentAsync(
                    environmentId,
                    currentUserId,
                    logger);

                if (result.CloudEnvironment == null)
                {
                    logger.AddDuration(duration)
                        .AddReason($"{result.HttpStatusCode}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(ShutdownCloudEnvironmentAsync)));

                    return StatusCode(result.HttpStatusCode, result.ErrorCode);
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(result.CloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(ShutdownCloudEnvironmentAsync)));

                return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result.CloudEnvironment));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .AddEnvironmentId(environmentId)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(ShutdownCloudEnvironmentAsync)), ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/start")]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartCloudEnvironmentAsync(
            [FromRoute] string environmentId)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var currentUserId = CurrentUserProvider.GetProfileId();
                var cloudEnvironment = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
                if (cloudEnvironment == null)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogWarning(GetType().FormatLogMessage(nameof(StartCloudEnvironmentAsync)));

                    return NotFound();
                }

                // Reroute to correct location if needed
                var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(cloudEnvironment.Location);
                if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
                {
                    return RedirectToLocation(owningStamp);
                }

                var accessToken = CurrentUserProvider.GetBearerToken();

                // Build the service URI.
                var displayUri = Request.GetDisplayUrl();
                var requestUri = displayUri.Substring(0, displayUri.IndexOf(StartEnvironmentHttpVerb));
                var serviceUri = ServiceUriBuilder.GetServiceUri(requestUri, owningStamp);
                var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(requestUri, owningStamp).ToString();

                var result = await EnvironmentManager.StartEnvironmentAsync(
                                                    environmentId,
                                                    serviceUri,
                                                    callbackUriFormat,
                                                    currentUserId,
                                                    accessToken,
                                                    logger);
                if (result.CloudEnvironment == null)
                {
                    // Couldn't be registered. Assume it already exists?
                    logger.AddDuration(duration)
                        .AddReason($"{result.HttpStatusCode}")
                        .LogError(GetType().FormatLogErrorMessage(nameof(StartCloudEnvironmentAsync)));

                    return StatusCode(result.HttpStatusCode, result.ErrorCode);
                }

                logger.AddDuration(duration)
                    .AddCloudEnvironment(result.CloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(StartCloudEnvironmentAsync)));

                return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result.CloudEnvironment));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(StartCloudEnvironmentAsync)), ex.Message);
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
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCloudEnvironmentAsync(
            [FromBody]CreateCloudEnvironmentBody createEnvironmentInput)
        {
            var logger = HttpContext.GetLogger();
            var duration = logger.StartDuration();

            try
            {
                var envName = createEnvironmentInput.FriendlyName.Trim();
                createEnvironmentInput.FriendlyName = envName;
                ValidationUtil.IsRequired(createEnvironmentInput, nameof(createEnvironmentInput));
                ValidationUtil.IsRequired(createEnvironmentInput.FriendlyName, nameof(createEnvironmentInput.FriendlyName));
                ValidationUtil.IsRequired(createEnvironmentInput.Type, nameof(createEnvironmentInput.Type));

                // SkuPlan ID required and valid.
                ValidationUtil.IsRequired(createEnvironmentInput.PlanId, nameof(createEnvironmentInput.PlanId));
                ValidationUtil.IsTrue(VsoPlanInfo.TryParse(createEnvironmentInput.PlanId, out _), "Invalid plan ID.");

                var cloudEnvironment = Mapper.Map<CreateCloudEnvironmentBody, CloudEnvironment>(createEnvironmentInput);

                // TODO HACK: specify a temporary sku. Old clients don't specify one.
                if (string.IsNullOrEmpty(cloudEnvironment.SkuName))
                {
                    cloudEnvironment.SkuName = LookupSkuName(ComputeOS.Linux, SkuTier.Standard);
                }

                // Legacy Linux SKU name conversion
                else if (cloudEnvironment.SkuName.Equals("smallLinuxPreview", StringComparison.OrdinalIgnoreCase))
                {
                    cloudEnvironment.SkuName = LookupSkuName(ComputeOS.Linux, SkuTier.Standard);
                }

                // Legacy Windows SKU name conversion
                else if (cloudEnvironment.SkuName.Equals("smallWindowsPreview", StringComparison.OrdinalIgnoreCase))
                {
                    cloudEnvironment.SkuName = LookupSkuName(ComputeOS.Windows, SkuTier.Standard);
                }

                // Reroute to correct location if needed
                var owningStamp = default(IControlPlaneStampInfo);
                try
                {
                    owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(cloudEnvironment.Location);
                    if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
                    {
                        return RedirectToLocation(owningStamp);
                    }
                }
                catch (NotSupportedException)
                {
                    var message = $"The requested location is not supported: {cloudEnvironment.Location}";
                    return CreateEnvironmentBadRequest(message, duration, logger);
                }

                // Validate the requested SKU
                if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku))
                {
                    var message = $"The requested SKU is not defined: {cloudEnvironment.SkuName}";
                    return CreateEnvironmentBadRequest(message, duration, logger);
                }

                if (!sku.Enabled)
                {
                    var message = $"The requested SKU is not available: {cloudEnvironment.SkuName}";
                    return CreateEnvironmentBadRequest(message, duration, logger);
                }

                // Validate the SKU location
                if (!sku.SkuLocations.Contains(cloudEnvironment.Location))
                {
                    var message = $"The requested SKU is not available in location: {cloudEnvironment.Location}";
                    return CreateEnvironmentBadRequest(message, duration, logger);
                }

                var currentUserId = CurrentUserProvider.GetProfileId();
                var currentUserProviderId = CurrentUserProvider.GetProfile().ProviderId;
                var accessToken = CurrentUserProvider.GetBearerToken();

                // Build the service URI.
                var serviceUri = ServiceUriBuilder.GetServiceUri(Request.GetDisplayUrl(), owningStamp);
                var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(Request.GetDisplayUrl(), owningStamp).ToString();

                var cloudEnvironmentOptions = new CloudEnvironmentOptions();
                if (createEnvironmentInput.ExperimentalFeatures != null)
                {
                    cloudEnvironmentOptions.CustomContainers = createEnvironmentInput.ExperimentalFeatures.CustomContainers;
                    cloudEnvironmentOptions.NewTerminal = createEnvironmentInput.ExperimentalFeatures.NewTerminal;
                }

                var createCloudEnvironmentResult = await EnvironmentManager.CreateEnvironmentAsync(
                    cloudEnvironment,
                    cloudEnvironmentOptions,
                    serviceUri,
                    callbackUriFormat,
                    currentUserId,
                    currentUserProviderId,
                    accessToken,
                    logger);

                if (createCloudEnvironmentResult.CloudEnvironment is null)
                {
                    // Couldn't be registered. Assume it already exists?
                    logger.AddDuration(duration)
                        .AddReason($"{HttpStatusCode.Conflict}: already exists?")
                        .LogError(GetType().FormatLogErrorMessage(nameof(CreateCloudEnvironmentAsync)));

                    // TODO: 409 conflict might mean that the requested session id already exists
                    // Could mean that the friendly-name is in conflict, could mean anything!
                    return StatusCode(createCloudEnvironmentResult.HttpStatusCode, createCloudEnvironmentResult.ErrorCode);
                }

                var location = new UriBuilder(Request.GetDisplayUrl());
                location.Path = location.Path.TrimEnd('/');
                location.Path = $"{location.Path}/{createCloudEnvironmentResult.CloudEnvironment.Id}";

                logger.AddDuration(duration)
                    .AddCloudEnvironment(createCloudEnvironmentResult.CloudEnvironment)
                    .LogInfo(GetType().FormatLogMessage(nameof(CreateCloudEnvironmentAsync)));

                return Created(location.Uri, Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(createCloudEnvironmentResult.CloudEnvironment));
            }
            catch (Exception ex)
            {
                logger.AddDuration(duration)
                    .LogErrorWithDetail(GetType().FormatLogErrorMessage(nameof(CreateCloudEnvironmentAsync)), ex.Message);

                if (ex is HttpResponseStatusException httpResponseStatusException)
                {
                    // If it was a 503 from the backend we want to pass that one down
                    if (httpResponseStatusException.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        if (httpResponseStatusException.RetryAfter.HasValue)
                        {
                            Response.Headers.Add("Retry-After", httpResponseStatusException.RetryAfter.Value.ToString());
                        }

                        return StatusCode(StatusCodes.Status503ServiceUnavailable);
                    }
                }

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

                // Lookup environment - TODO: prevent duplicate DB lookups when updating the record
                var environment = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
                if (environment == null)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogWarning(GetType().FormatLogMessage(nameof(DeleteCloudEnvironmentAsync)));

                    return NotFound();
                }

                // Reroute to correct location if needed
                var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
                if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
                {
                    return RedirectToLocation(owningStamp);
                }

                // We are in the right location, go ahead and delete
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
            [FromBody]EnvironmentRegistrationCallbackBody callbackBody)
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

                // Lookup environment - TODO: prevent duplicate DB lookups when updating the record
                var environment = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger);
                if (environment == null)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.NotFound}")
                        .LogWarning(GetType().FormatLogMessage(nameof(UpdateEnvironmentCallbackAsync)));

                    return NotFound();
                }

                // The callback should only ever be received by the owning location, since the callback is constructed
                // with a location-specific uri. Reject the call if it comes into the wrong location.
                var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
                if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
                {
                    logger.AddDuration(duration)
                        .AddEnvironmentId(environmentId)
                        .AddReason($"{HttpStatusCode.BadRequest}")
                        .LogWarning(GetType().FormatLogMessage(nameof(UpdateEnvironmentCallbackAsync)));

                    return BadRequest();
                }

                // We are in the right location, go ahead and update the callback
                var options = Mapper.Map<EnvironmentRegistrationCallbackBody, EnvironmentRegistrationCallbackOptions>(callbackBody);
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

        private IActionResult RedirectToLocation(IControlPlaneStampInfo owningStamp)
        {
            // Return a 307 to the location-specific hostname for the owning location.
            var builder = new UriBuilder()
            {
                Host = owningStamp.DnsHostName,
                Path = Request.Path,
                Query = Request.QueryString.Value,
                Scheme = Uri.UriSchemeHttps,
            };
            return new RedirectResult(builder.ToString(), permanent: false, preserveMethod: true);
        }

        private IActionResult CreateEnvironmentBadRequest(
            string message,
            Duration duration,
            IDiagnosticsLogger logger)
        {
            logger.AddDuration(duration)
                .AddReason($"{HttpStatusCode.BadRequest}: {message}")
                .LogError(GetType().FormatLogErrorMessage(nameof(CreateCloudEnvironmentAsync)));
            return BadRequest(message);
        }

        private string LookupSkuName(ComputeOS computeOS, SkuTier skuTier)
        {
            return SkuCatalog.CloudEnvironmentSkus
                .Where(defaultSku => defaultSku.Value.ComputeOS == computeOS)
                .Where(defaultSku => defaultSku.Value.Tier == skuTier)
                .Where(defaultSku => defaultSku.Value.Enabled)
                .Single()
                .Value
                .SkuName;
        }
    }
}
