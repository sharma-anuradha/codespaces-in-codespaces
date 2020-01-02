// <copyright file="EnvironmentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;

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
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet("{environmentId}")]
        [ThrottlePerUserHigh(nameof(EnvironmentsController), nameof(GetAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var currentUserId = CurrentUserProvider.GetProfileId();

            var result = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUserId, logger.NewChildLogger());
            if (result is null)
            {
                return NotFound();
            }

            logger.AddCloudEnvironment(result);

            return Ok(Mapper.Map<CloudEnvironmentResult>(result));
        }

        /// <summary>
        /// Lists all cloud environments belonging to the the current user and environment name (optional).
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="planId">Target plan id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(ListAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpOperationalScope("list")]
        public async Task<IActionResult> ListAsync(
            [FromQuery]string name,
            [FromQuery]string planId,
            [FromServices]IDiagnosticsLogger logger)
        {
            var currentUserId = CurrentUserProvider.GetProfileId();

            var modelsRaw = await EnvironmentManager.ListEnvironmentsAsync(
                currentUserId, name, planId, logger.NewChildLogger());

            logger.FluentAddValue("Count", modelsRaw.Count().ToString());

            return Ok(Mapper.Map<CloudEnvironmentResult[]>(modelsRaw));
        }

        /// <summary>
        /// Shutdown a cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/shutdown")]
        [ThrottlePerUserHigh(nameof(EnvironmentsController), nameof(ShutdownAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("shutdown")]
        public async Task<IActionResult> ShutdownAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var currentUserId = CurrentUserProvider.GetProfileId();

            var environment = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUserId, logger.NewChildLogger());
            if (environment == null)
            {
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
                environmentId, currentUserId, logger.NewChildLogger());
            if (result.CloudEnvironment == null)
            {
                logger.AddReason($"{result.HttpStatusCode}");

                return StatusCode(result.HttpStatusCode, result.MessageCode);
            }

            logger.AddCloudEnvironment(result.CloudEnvironment);

            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result.CloudEnvironment));
        }

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/start")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(StartAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("start")]
        public async Task<IActionResult> StartAsync(
            [FromRoute] string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            var currentUserId = CurrentUserProvider.GetProfileId();

            var cloudEnvironment = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUserId, logger.NewChildLogger());
            if (cloudEnvironment == null)
            {
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
            var requestUri = displayUri.Substring(0, displayUri.IndexOf(environmentId));
            var serviceUri = ServiceUriBuilder.GetServiceUri(requestUri, owningStamp);
            var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(requestUri, owningStamp).ToString();

            var result = await EnvironmentManager.StartEnvironmentAsync(
                environmentId, serviceUri, callbackUriFormat, currentUserId, accessToken, logger.NewChildLogger());
            if (result.CloudEnvironment == null)
            {
                return StatusCode(result.HttpStatusCode, result.MessageCode);
            }

            logger.AddCloudEnvironment(result.CloudEnvironment);

            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result.CloudEnvironment));
        }

        /// <summary>
        /// Create a new cloud environment.
        /// </summary>
        /// <param name="createEnvironmentInput">The cloud environment input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(CreateAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [HttpOperationalScope("create")]
        public async Task<IActionResult> CreateAsync(
            [FromBody]CreateCloudEnvironmentBody createEnvironmentInput,
            [FromServices]IDiagnosticsLogger logger)
        {
            var envName = createEnvironmentInput.FriendlyName.Trim();
            createEnvironmentInput.FriendlyName = envName;
            ValidationUtil.IsRequired(createEnvironmentInput, nameof(createEnvironmentInput));
            ValidationUtil.IsRequired(createEnvironmentInput.FriendlyName, nameof(createEnvironmentInput.FriendlyName));
            ValidationUtil.IsRequired(createEnvironmentInput.Type, nameof(createEnvironmentInput.Type));

            // Regex pattern for naming, can include alphanumeric, space, underscore, parentheses, hyphen, period, and Unicode characters that match the allowed characters.
            var regex = new Regex(@"^[-\w\._\(\) ]{1,90}$", RegexOptions.IgnoreCase);
            ValidationUtil.IsTrue(regex.IsMatch(envName));

            // SkuPlan ID required and valid.
            ValidationUtil.IsRequired(createEnvironmentInput.PlanId, nameof(createEnvironmentInput.PlanId));
            ValidationUtil.IsTrue(VsoPlanInfo.TryParse(createEnvironmentInput.PlanId, out _), "Invalid plan ID.");

            var cloudEnvironment = Mapper.Map<CreateCloudEnvironmentBody, CloudEnvironment>(createEnvironmentInput);
            ValidationUtil.IsRequired(createEnvironmentInput.SkuName, nameof(createEnvironmentInput.SkuName));

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
                var message = $"{HttpStatusCode.BadRequest}: The requested location is not supported: {cloudEnvironment.Location}";
                logger.AddReason(message);
                return BadRequest(message);
            }

            var currentUser = CurrentUserProvider.GetProfile();

            // Validate the requested Sku
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku)
                || !ProfileUtils.IsSkuVisibleToProfile(currentUser, sku))
            {
                var message = $"{HttpStatusCode.BadRequest}: The requested SKU is not defined: {cloudEnvironment.SkuName}";
                logger.AddReason(message);
                return BadRequest(message);
            }

            // Validate the Sku is enabled
            if (!sku.Enabled)
            {
                var message = $"{HttpStatusCode.BadRequest}: The requested SKU is not available: {cloudEnvironment.SkuName}";
                logger.AddReason(message);
                return BadRequest(message);
            }

            // Validate the Sku location
            if (!sku.SkuLocations.Contains(cloudEnvironment.Location))
            {
                var message = $"{HttpStatusCode.BadRequest}: The requested SKU is not available in location: {cloudEnvironment.Location}";
                logger.AddReason(message);
                return BadRequest(message);
            }

            var currentUserId = currentUser.Id;
            var currentUserProviderId = currentUser.ProviderId;
            var accessToken = CurrentUserProvider.GetBearerToken();

            // Build the service URI.
            var serviceUri = ServiceUriBuilder.GetServiceUri(Request.GetDisplayUrl(), owningStamp);
            var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(Request.GetDisplayUrl(), owningStamp).ToString();

            var cloudEnvironmentOptions = new CloudEnvironmentOptions();
            if (createEnvironmentInput.ExperimentalFeatures != null)
            {
                cloudEnvironmentOptions.CustomContainers = createEnvironmentInput.ExperimentalFeatures.CustomContainers;
                cloudEnvironmentOptions.NewTerminal = createEnvironmentInput.ExperimentalFeatures.NewTerminal;
                cloudEnvironmentOptions.EnableMultipleWorkspaces = createEnvironmentInput.ExperimentalFeatures.EnableMultipleWorkspaces;
            }

            // Create the environement
            var createCloudEnvironmentResult = default(CloudEnvironmentServiceResult);
            try
            {
                createCloudEnvironmentResult = await EnvironmentManager.CreateEnvironmentAsync(
                    cloudEnvironment, cloudEnvironmentOptions, serviceUri, callbackUriFormat, currentUserId, currentUserProviderId, accessToken, logger.NewChildLogger());
            }
            catch (HttpResponseStatusException e)
            {
                // If it was a 503 from the backend we want to pass that one down
                if (e.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    if (e.RetryAfter.HasValue)
                    {
                        Response.Headers.Add("Retry-After", e.RetryAfter.Value.ToString());
                    }

                    return StatusCode(StatusCodes.Status503ServiceUnavailable);
                }
            }

            if (createCloudEnvironmentResult.CloudEnvironment is null)
            {
                // TODO: 409 conflict might mean that the requested session id already exists
                // Could mean that the friendly-name is in conflict, could mean anything!
                // Couldn't be registered. Assume it already exists?
                return StatusCode(createCloudEnvironmentResult.HttpStatusCode, createCloudEnvironmentResult.MessageCode);
            }

            var location = new UriBuilder(Request.GetDisplayUrl());
            location.Path = location.Path.TrimEnd('/');
            location.Path = $"{location.Path}/{createCloudEnvironmentResult.CloudEnvironment.Id}";

            logger.AddCloudEnvironment(createCloudEnvironmentResult.CloudEnvironment);

            return Created(location.Uri, Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(createCloudEnvironmentResult.CloudEnvironment));
        }

        /// <summary>
        /// Delete a cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code <see cref="HttpStatusCode.NoContent"/> if deleted, otherwise <see cref="HttpStatusCode.NotFound"/>.</returns>
        [HttpDelete("{environmentId}")]
        [ThrottlePerUserHigh(nameof(EnvironmentsController), nameof(DeleteAsync))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("delete")]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var currentUserId = CurrentUserProvider.GetProfileId();

            // Lookup environment - TODO: prevent duplicate DB lookups when updating the record
            var environment = await EnvironmentManager.GetEnvironmentAsync(environmentId, currentUserId, logger.NewChildLogger());
            if (environment == null)
            {
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
                environmentId, currentUserId, logger.NewChildLogger());
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        /// <summary>
        /// Register a callback for an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="callbackBody">The callback info.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result that containts the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost("{environmentId}/_callback")] // TODO: This should be PATCH not POST
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(UpdateAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("update")]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute]string environmentId,
            [FromBody]EnvironmentRegistrationCallbackBody callbackBody,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));
            ValidationUtil.IsRequired(callbackBody, nameof(callbackBody));
            ValidationUtil.IsRequired(callbackBody?.Payload, nameof(callbackBody.Payload));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionId, nameof(callbackBody.Payload.SessionId));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionPath, nameof(callbackBody.Payload.SessionPath));

            var currentUserId = CurrentUserProvider.GetProfileId();

            // Lookup environment - TODO: prevent duplicate DB lookups when updating the record
            var environment = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUserId, logger.NewChildLogger());
            if (environment == null)
            {
                return NotFound();
            }

            // The callback should only ever be received by the owning location, since the callback is constructed
            // with a location-specific uri. Reject the call if it comes into the wrong location.
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return BadRequest();
            }

            // We are in the right location, go ahead and update the callback
            var options = Mapper.Map<EnvironmentRegistrationCallbackBody, EnvironmentRegistrationCallbackOptions>(callbackBody);
            var result = await EnvironmentManager.UpdateEnvironmentCallbackAsync(
                environmentId, options, currentUserId, logger.NewChildLogger());
            if (result is null)
            {
                logger.AddSessionId(callbackBody.Payload.SessionId)
                    .AddReason($"{HttpStatusCode.NotFound}");

                return NotFound();
            }

            logger.AddCloudEnvironment(result);

            return Ok(Mapper.Map<CloudEnvironmentResult>(result));
        }

        /// <summary>
        /// Update settings of an existing cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="updateEnvironmentInput">The new environment settings.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result that contains the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPatch("{environmentId}")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(UpdateSettingsAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("update_settings")]
        public async Task<IActionResult> UpdateSettingsAsync(
            [FromRoute]string environmentId,
            [FromBody]UpdateCloudEnvironmentBody updateEnvironmentInput,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));
            ValidationUtil.IsRequired(updateEnvironmentInput, nameof(updateEnvironmentInput));
            ValidationUtil.IsRequired(logger, nameof(logger));

            var currentUserId = CurrentUserProvider.GetProfileId();

            var environment = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUserId, logger.NewChildLogger());
            if (environment == null)
            {
                return NotFound();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var updateRequest = Mapper.Map<CloudEnvironmentUpdate>(updateEnvironmentInput);

            var result = await EnvironmentManager.UpdateEnvironmentSettingsAsync(environment.Id, updateRequest, CurrentUserProvider, logger);

            if (result.IsSuccess)
            {
                return Ok(Mapper.Map<CloudEnvironmentResult>(result.CloudEnvironment));
            }
            else
            {
                return BadRequest(result.ValidationErrors);
            }
        }

        /// <summary>
        /// Get the list of settings which are allowed to be updated on the given environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result that contains the <see cref="CloudEnvironmentAvailableUpdatesResult"/>.</returns>
        [HttpGet("{environmentId}/updates")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(GetAvailableUpdatesAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("available_updates")]
        public async Task<IActionResult> GetAvailableUpdatesAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);

            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var currentUser = CurrentUserProvider.GetProfile();

            var environment = await EnvironmentManager.GetEnvironmentAsync(
                environmentId, currentUser.Id, logger.NewChildLogger());
            if (environment == null)
            {
                return NotFound();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var availableUpdates = EnvironmentManager.GetEnvironmentAvailableSettingsUpdates(environment, currentUser, logger.NewChildLogger());

            var result = new CloudEnvironmentAvailableUpdatesResult();

            if (availableUpdates.AllowedAutoShutdownDelayMinutes != null && availableUpdates.AllowedAutoShutdownDelayMinutes.Any())
            {
                result.AllowedAutoShutdownDelayMinutes = availableUpdates.AllowedAutoShutdownDelayMinutes;
            }
            else
            {
                result.AllowedAutoShutdownDelayMinutes = Array.Empty<int>();
            }

            if (availableUpdates.AllowedSkus != null && availableUpdates.AllowedSkus.Any())
            {
                result.AllowedSkus = availableUpdates.AllowedSkus.Select((sku) => new SkuInfoResult
                {
                    Name = sku.SkuName,
                    DisplayName = sku.DisplayName,
                    OS = sku.ComputeOS.ToString(),
                }).ToArray();
            }
            else
            {
                result.AllowedSkus = Array.Empty<SkuInfoResult>();
            }

            return Ok(result);
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
    }
}
