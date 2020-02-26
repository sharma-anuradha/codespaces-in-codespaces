// <copyright file="EnvironmentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;
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
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// The cloud environment API controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("environments_controller")]
    public class EnvironmentsController : ControllerBase /* TODO add this later IEnvironmentsHttpContract */
    {
        private const string LoggingBaseName = "environments_controller";
        private const string UnauthorizedPlanId = "unauthorized_plan_id";
        private const string UnauthorizedPlanScope = "unauthorized_plan_scope";
        private const string UnauthorizedPlanUser = "unauthorized_plan_user";
        private const string UnauthorizedEnvironmentUser = "unauthorized_environment_user";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentsController"/> class.
        /// </summary>
        /// <param name="environmentManager">The front-end environment manager.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="controlPlaneInfo">The control-plane info.</param>
        /// <param name="currentLocationProvider">The current location provider.</param>
        /// <param name="skuCatalog">The sku catalog.</param>
        /// <param name="mapper">The configured auto-mapper.</param>
        /// <param name="serviceUriBuilder">The service uri builder.</param>
        /// <param name="frontEndAppSettings">Front-end app settings.</param>
        public EnvironmentsController(
            IEnvironmentManager environmentManager,
            IPlanManager planManager,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider,
            ISkuCatalog skuCatalog,
            IMapper mapper,
            IServiceUriBuilder serviceUriBuilder,
            FrontEndAppSettings frontEndAppSettings)
        {
            EnvironmentManager = Requires.NotNull(environmentManager, nameof(environmentManager));
            PlanManager = Requires.NotNull(planManager, nameof(planManager));
            CurrentUserProvider = Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            CurrentLocationProvider = Requires.NotNull(currentLocationProvider, nameof(currentLocationProvider));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            Mapper = Requires.NotNull(mapper, nameof(mapper));
            ServiceUriBuilder = Requires.NotNull(serviceUriBuilder, nameof(serviceUriBuilder));
            FrontEndAppSettings = Requires.NotNull(frontEndAppSettings, nameof(frontEndAppSettings));
        }

        private IEnvironmentManager EnvironmentManager { get; }

        private IPlanManager PlanManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        private ISkuCatalog SkuCatalog { get; }

        private IMapper Mapper { get; }

        private IServiceUriBuilder ServiceUriBuilder { get; }

        private FrontEndAppSettings FrontEndAppSettings { get; }

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
            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(
                environment, nonOwnerScope: PlanAccessTokenScopes.ReadEnvironments, logger))
            {
                return new ForbidResult();
            }

            logger.AddCloudEnvironment(environment);

            return Ok(Mapper.Map<CloudEnvironmentResult>(environment));
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
            // In the case of a authentication using a plan access token, infer the plan from the token if not set
            planId ??= HttpContext.GetPlan();

            if (HttpContext.IsPlanAuthorized(planId) == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                logger.LogWarning(UnauthorizedPlanId);
                return new ForbidResult();
            }

            UserIdSet userIdSet;
            if (HttpContext.IsScopeAuthorized(PlanAccessTokenScopes.ReadEnvironments) == true)
            {
                // A user with this explicit scope is authorized to list
                // all users' environments in the plan.
                userIdSet = null;
            }
            else if (HttpContext.IsScopeAuthorized(PlanAccessTokenScopes.WriteEnvironments) != false)
            {
                // A user with only write scope (or an unscoped token) is authorized only
                // to list their own environments.
                userIdSet = CurrentUserProvider.GetCurrentUserIdSet();
            }
            else
            {
                logger.LogWarning(UnauthorizedPlanScope);
                return new ForbidResult();
            }

            var modelsRaw = await EnvironmentManager.ListAsync(
                logger.NewChildLogger(), planId, name, userIdSet);

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
        [ThrottlePerUserHigh(nameof(EnvironmentsController), nameof(SuspendAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("shutdown")]
        public async Task<IActionResult> SuspendAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(environment, nonOwnerScope: null, logger))
            {
                return new ForbidResult();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            // We are in the right location, go ahead and shutdown
            var result = await EnvironmentManager.SuspendAsync(
                environment, logger.NewChildLogger());
            if (result.CloudEnvironment == null)
            {
                logger.AddReason($"{result.HttpStatusCode}");

                return StatusCode(result.HttpStatusCode, result.MessageCode);
            }

            logger.AddCloudEnvironment(result.CloudEnvironment);

            try
            {
                var userId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
                var targetResource = new TargetResource("environment", result.CloudEnvironment.Id);
                logger.Audit(
                    AuditScope.Application,
                    "EnvironmentShutdown",
                    AuditEventCategory.ObjectManagement,
                    new CallerIdentity(CallerIdentityType.ObjectID, userId),
                    targetResource,
                    OperationResult.Success);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LoggingBaseName}_audit_error", ex);
            }

            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result.CloudEnvironment));
        }

        /// <summary>
        /// Starts a shutdown environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/start")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(ResumeAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("start")]
        public async Task<IActionResult> ResumeAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(environment, nonOwnerScope: null, logger))
            {
                return new ForbidResult();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var startEnvParams = GetStartCloudEnvironmentParameters();

            var result = await EnvironmentManager.ResumeAsync(
                environment,
                startEnvParams,
                logger.NewChildLogger());

            if (result.CloudEnvironment == null)
            {
                return StatusCode(result.HttpStatusCode, result.MessageCode);
            }

            logger.AddCloudEnvironment(result.CloudEnvironment);

            try
            {
                var userId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
                var targetResource = new TargetResource("environment", result.CloudEnvironment.Id);
                logger.Audit(
                    AuditScope.Application,
                    "EnvironmentStart",
                    AuditEventCategory.ObjectManagement,
                    new CallerIdentity(CallerIdentityType.ObjectID, userId),
                    targetResource,
                    OperationResult.Success);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LoggingBaseName}_audit_error", ex);
            }

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

            var currentUserProfile = CurrentUserProvider.GetProfile();
            var currentUserIdSet = CurrentUserProvider.GetCurrentUserIdSet();

            // Validate the requested Sku
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(cloudEnvironment.SkuName, out var sku)
                || !ProfileUtils.IsSkuVisibleToProfile(currentUserProfile, sku))
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

            ValidationUtil.IsRequired(cloudEnvironment.PlanId, nameof(CloudEnvironment.PlanId));

            // Validate that the specified plan ID is well-formed.
            ValidationUtil.IsTrue(
                VsoPlanInfo.TryParse(cloudEnvironment.PlanId, out var plan),
                $"Invalid plan ID: {cloudEnvironment.PlanId}");

            // Validate the plan exists (and lookup the plan details).
            plan.Location = cloudEnvironment.Location;
            var planDetails = (await PlanManager.GetAsync(plan, logger)).VsoPlan;
            ValidationUtil.IsTrue(planDetails != null, $"Plan {cloudEnvironment.PlanId} not found.");

            if (!AuthorizePlanAccess(planDetails, PlanAccessTokenScopes.WriteEnvironments, logger))
            {
                return new ForbidResult();
            }

            var cloudEnvironmentOptions = new CloudEnvironmentOptions();
            if (createEnvironmentInput.ExperimentalFeatures != null)
            {
                cloudEnvironmentOptions.CustomContainers = createEnvironmentInput.ExperimentalFeatures.CustomContainers;
                cloudEnvironmentOptions.NewTerminal = createEnvironmentInput.ExperimentalFeatures.NewTerminal;
            }

            // Create the environement
            var createCloudEnvironmentResult = default(CloudEnvironmentServiceResult);
            try
            {
                var startEnvParams = GetStartCloudEnvironmentParameters();

                cloudEnvironment.OwnerId = currentUserIdSet.PreferredUserId;

                createCloudEnvironmentResult = await EnvironmentManager.CreateAsync(
                    cloudEnvironment,
                    cloudEnvironmentOptions,
                    startEnvParams,
                    plan,
                    logger.NewChildLogger());
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

            try
            {
                var userId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
                var targetResource = new TargetResource("environment", createCloudEnvironmentResult.CloudEnvironment.Id);
                logger.Audit(
                    AuditScope.Application,
                    "EnvironmentCreate",
                    AuditEventCategory.ObjectManagement,
                    new CallerIdentity(CallerIdentityType.ObjectID, userId),
                    targetResource,
                    OperationResult.Success);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LoggingBaseName}_audit_error", ex);
            }

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
            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(
                environment, nonOwnerScope: PlanAccessTokenScopes.DeleteEnvironments, logger))
            {
                return new ForbidResult();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            // We are in the right location, go ahead and delete
            var result = await EnvironmentManager.DeleteAsync(
                environment, logger.NewChildLogger());
            if (!result)
            {
                return NotFound();
            }

            try
            {
                var userId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
                var targetResource = new TargetResource("environment", environment.Id);
                logger.Audit(
                    AuditScope.Application,
                    "EnvironmentDelete",
                    AuditEventCategory.ObjectManagement,
                    new CallerIdentity(CallerIdentityType.ObjectID, userId),
                    targetResource,
                    OperationResult.Success);
            }
            catch (Exception ex)
            {
                logger.LogException($"{LoggingBaseName}_audit_error", ex);
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
            ValidationUtil.IsRequired(callbackBody, nameof(callbackBody));
            ValidationUtil.IsRequired(callbackBody?.Payload, nameof(callbackBody.Payload));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionId, nameof(callbackBody.Payload.SessionId));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionPath, nameof(callbackBody.Payload.SessionPath));

            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            var currentUserIdSet = CurrentUserProvider.GetCurrentUserIdSet();
            if (!currentUserIdSet.EqualsAny(environment.OwnerId))
            {
                return new ForbidResult();
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
            var result = await EnvironmentManager.UpdateCallbackAsync(
                environment, options, logger.NewChildLogger());
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
            ValidationUtil.IsRequired(updateEnvironmentInput, nameof(updateEnvironmentInput));
            ValidationUtil.IsRequired(logger, nameof(logger));

            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(environment, nonOwnerScope: null, logger))
            {
                return new ForbidResult();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var updateRequest = Mapper.Map<CloudEnvironmentUpdate>(updateEnvironmentInput);

            var result = await EnvironmentManager.UpdateSettingsAsync(environment, updateRequest, logger);

            if (result.IsSuccess)
            {
                try
                {
                    var userId = CurrentUserProvider.GetCurrentUserIdSet().PreferredUserId;
                    var targetResource = new TargetResource("environment", environment.Id);
                    logger.Audit(
                        AuditScope.Application,
                        "EnvironmentUpdate",
                        AuditEventCategory.ObjectManagement,
                        new CallerIdentity(CallerIdentityType.ObjectID, userId),
                        targetResource,
                        OperationResult.Success);
                }
                catch (Exception ex)
                {
                    logger.LogException($"{LoggingBaseName}_audit_error", ex);
                }

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
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(GetAvailableSettingsUpdatesAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("available_updates")]
        public async Task<IActionResult> GetAvailableSettingsUpdatesAsync(
            [FromRoute]string environmentId,
            [FromServices]IDiagnosticsLogger logger)
        {
            var environment = await GetEnvironmentAsync(environmentId, logger);
            if (environment is null)
            {
                return NotFound();
            }

            if (!AuthorizeEnvironmentAccess(environment, nonOwnerScope: null, logger))
            {
                return new ForbidResult();
            }

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var availableUpdates = await EnvironmentManager.GetAvailableSettingsUpdatesAsync(environment, logger.NewChildLogger());

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
                var currentUser = CurrentUserProvider.GetProfile();

                var visibleSkus = availableUpdates.AllowedSkus
                    .Where((sku) => ProfileUtils.IsSkuVisibleToProfile(currentUser, sku));

                result.AllowedSkus = visibleSkus
                    .Select((sku) => new SkuInfoResult
                    {
                        Name = sku.SkuName,
                        DisplayName = sku.DisplayName,
                        OS = sku.ComputeOS.ToString(),
                    })
                    .ToArray();
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

        private async Task<CloudEnvironment> GetEnvironmentAsync(
            string environmentId,
            IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);
            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var environment = await EnvironmentManager.GetAndStateRefreshAsync(environmentId, logger);

            return environment;
        }

        /// <summary>
        /// Checks if the current user is authorized to access an environment.
        /// </summary>
        /// <param name="environment">Environment that the user is attempting to access.</param>
        /// <param name="nonOwnerScope">Scope that the user is required to have IF they
        /// do not have full owner-level access to the environment. Or null if only
        /// owners should be authorized.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>True if the user is authorized, else false.</returns>
        private bool AuthorizeEnvironmentAccess(
            CloudEnvironment environment,
            string nonOwnerScope,
            IDiagnosticsLogger logger)
        {
            if (HttpContext.IsPlanAuthorized(environment.PlanId) == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                logger.LogWarning(UnauthorizedPlanId);
                return false;
            }

            var currentUserIdSet = CurrentUserProvider.GetCurrentUserIdSet();
            if (currentUserIdSet.EqualsAny(environment.OwnerId) &&
                HttpContext.IsScopeAuthorized(PlanAccessTokenScopes.WriteEnvironments) != false)
            {
                // Users with write access to a plan (or an unscoped access token)
                // have full access to their own environments in the plan.
                return true;
            }

            if (nonOwnerScope != null)
            {
                if (HttpContext.IsScopeAuthorized(nonOwnerScope) == true)
                {
                    // Users with certain explicit scopes can have limited access
                    // to environments they don't own in the plan.
                    return true;
                }

                logger.LogWarning(UnauthorizedPlanScope);
            }
            else
            {
                logger.LogWarning(UnauthorizedEnvironmentUser);
            }

            return false;
        }

        /// <summary>
        /// Checks if the current user is authorized to access a plan.
        /// </summary>
        /// <param name="plan">Plan that the user is attempting to access.</param>
        /// <param name="requiredScope">Scope that the user must have, if they have a scoped
        /// access token.</param>
        /// <param name="logger">Diagnostic logger.</param>
        /// <returns>True if the user is authorized, else false.</returns>
        private bool AuthorizePlanAccess(
            VsoPlan plan,
            string requiredScope,
            IDiagnosticsLogger logger)
        {
            bool? isPlanAuthorized = HttpContext.IsPlanAuthorized(plan.Plan.ResourceId);
            if (isPlanAuthorized == false)
            {
                // Users with explicit access to a different plan do not have access to this plan.
                logger.LogWarning(UnauthorizedPlanId);
                return false;
            }

            bool? isScopeAuthorized = HttpContext.IsScopeAuthorized(requiredScope);
            if (isScopeAuthorized == true)
            {
                // The user has the explicit required scope.
                return true;
            }
            else if (isScopeAuthorized == false)
            {
                // Users with a scoped access token must have the required scope.
                logger.LogWarning(UnauthorizedPlanScope);
                return false;
            }

            if (plan.UserId != null)
            {
                // Users without a scoped access token must be the owner of the plan
                // (if the plan has an owner).
                var currentUserIdSet = CurrentUserProvider.GetCurrentUserIdSet();
                if (!currentUserIdSet.EqualsAny(plan.UserId))
                {
                    logger.LogWarning(UnauthorizedPlanUser);
                    return false;
                }
            }
            else if (isPlanAuthorized != true)
            {
                // Users must have explicit authorization for unowned plans.
                logger.LogWarning(UnauthorizedPlanId);
                return false;
            }

            return true;
        }

        private StartCloudEnvironmentParameters GetStartCloudEnvironmentParameters()
        {
            var currentStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(CurrentLocationProvider.CurrentLocation);

            var displayUri = Request.GetDisplayUrl();

            // Extract the root API uri which is everything up and and including "/environments/"
            // TODO: Ideally this shouldn't need to include the "/environments/"
            var indexOfController = displayUri.IndexOf("/environments/", StringComparison.OrdinalIgnoreCase);
            var requestUri = indexOfController == -1
                ? displayUri
                : displayUri.Substring(0, indexOfController + "/environments/".Length);

            var serviceUri = ServiceUriBuilder.GetServiceUri(requestUri, currentStamp);
            var callbackUriFormat = ServiceUriBuilder.GetCallbackUriFormat(requestUri, currentStamp).ToString();

            return new StartCloudEnvironmentParameters
            {
                UserProfile = CurrentUserProvider.GetProfile(),
                FrontEndServiceUri = serviceUri,
                ConnectionServiceUri = new Uri(FrontEndAppSettings.VSLiveShareApiEndpoint, UriKind.Absolute),
                CallbackUriFormat = callbackUriFormat,
            };
        }
    }
}
