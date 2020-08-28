// <copyright file="EnvironmentsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Common.Identity;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Audit;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Metrics;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Environments;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Subscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Susbscriptions;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;

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
        private const int MaxEnvironmentVariablesSecrets = 10;

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
        /// <param name="skuUtils">skuUtils to find sku's eligiblity.</param>
        /// <param name="tokenProvider">Token Provider.</param>
        /// <param name="metricsManager">The metrics manager.</param>
        /// <param name="subscriptionManager">The subscription manager.</param>
        /// <param name="accessTokenReader">JWT reader configured for access tokens.</param>
        /// <param name="environmentAccessManager">The environment access manager.</param>
        /// <param name="environmentStateManager">The environment state manager.</param>
        /// <param name="gitHubFixedPlansMapper">The GitHub plan mapper.</param>
        public EnvironmentsController(
            IEnvironmentManager environmentManager,
            IPlanManager planManager,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider,
            ISkuCatalog skuCatalog,
            IMapper mapper,
            IServiceUriBuilder serviceUriBuilder,
            FrontEndAppSettings frontEndAppSettings,
            ISkuUtils skuUtils,
            ITokenProvider tokenProvider,
            IMetricsManager metricsManager,
            ISubscriptionManager subscriptionManager,
            ICascadeTokenReader accessTokenReader,
            IEnvironmentAccessManager environmentAccessManager,
            IEnvironmentStateManager environmentStateManager,
            GitHubFixedPlansMapper gitHubFixedPlansMapper)
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
            SkuUtils = Requires.NotNull(skuUtils, nameof(skuUtils));
            TokenProvider = Requires.NotNull(tokenProvider, nameof(tokenProvider));
            MetricsManager = Requires.NotNull(metricsManager, nameof(metricsManager));
            SubscriptionManager = Requires.NotNull(subscriptionManager, nameof(subscriptionManager));
            AccessTokenReader = Requires.NotNull(accessTokenReader, nameof(accessTokenReader));
            EnvironmentAccessManager = Requires.NotNull(environmentAccessManager, nameof(environmentAccessManager));
            EnvironmentStateManager = environmentStateManager;
            GitHubFixedPlansMapper = Requires.NotNull(gitHubFixedPlansMapper, nameof(gitHubFixedPlansMapper));
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

        private ISkuUtils SkuUtils { get; }

        private ITokenProvider TokenProvider { get; }

        private IMetricsManager MetricsManager { get; }

        private ISubscriptionManager SubscriptionManager { get; }

        private ICascadeTokenReader AccessTokenReader { get; }

        private IEnvironmentAccessManager EnvironmentAccessManager { get; }

        private IEnvironmentStateManager EnvironmentStateManager { get; }

        private GitHubFixedPlansMapper GitHubFixedPlansMapper { get; }

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
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> GetAsync(
            [Required, FromRoute] Guid environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            var environment = await EnvironmentManager.GetAsync(environmentId, logger);
            await ValidateEnvironmentIsNotSoftDeleted(environment, logger);

            // Normalize state
            environment = await EnvironmentStateManager.NormalizeEnvironmentStateAsync(environment, logger.NewChildLogger());

            return Ok(Mapper.Map<CloudEnvironmentResult>(environment));
        }

        /// <summary>
        /// Lists all cloud environments belonging to the the current user and environment name (optional).
        /// </summary>
        /// <param name="name">Target name.</param>
        /// <param name="planId">Target plan id.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="deleted">Indicates whether list is active/deleted environments.</param>
        /// <returns>An object result containing the list of <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(ListAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpOperationalScope("list")]
        [Audit(AuditEventCategory.ResourceManagement, targetResourceName: "User")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> ListAsync(
            [FromQuery] string name,
            [FromQuery] string planId,
            [FromServices] IDiagnosticsLogger logger,
            [FromQuery] bool deleted = false)
        {
            AuditAttribute.SetTargetResourceId(HttpContext, CurrentUserProvider?.CurrentUserIdSet?.PreferredUserId);
            var deletedFilter = deleted ? EnvironmentListType.DeletedEnvironments : EnvironmentListType.ActiveEnvironments;
            var environmentsList = await EnvironmentManager.ListAsync(planId, name, null, deletedFilter, logger.NewChildLogger());

            return Ok(Mapper.Map<CloudEnvironmentResult[]>(environmentsList));
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
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("shutdown")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> SuspendAsync(
            [FromRoute] Guid environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            await ValidateEnvironmentIsNotSoftDeleted(environmentId, logger);

            var result = await EnvironmentManager.SuspendAsync(
                environmentId, logger.NewChildLogger());

            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result));
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
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status503ServiceUnavailable)]
        [HttpOperationalScope("start")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        [QuotaHeader]
        public async Task<IActionResult> ResumeAsync(
            [FromRoute] Guid environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            await ValidateEnvironmentIsNotSoftDeleted(environmentId, logger);

            // Manually read the request body
            ResumeCloudEnvironmentBody requestBody;
            try
            {
                StreamReader bodyReader = new StreamReader(Request.Body);
                requestBody = JsonConvert.DeserializeObject<ResumeCloudEnvironmentBody>(await bodyReader.ReadToEndAsync());
            }
            catch
            {
                logger.AddReason($"{HttpStatusCode.BadRequest}: The request body was not able to be parsed.");
                return BadRequest();
            }

            IsSecretQuotaReached(requestBody?.Secrets);

            StartCloudEnvironmentParameters startEnvParams = await GetStartCloudEnvironmentParametersAsync();
            startEnvParams.Secrets = requestBody?.Secrets;

            var result = await EnvironmentManager.ResumeAsync(
                environmentId,
                startEnvParams,
                logger.NewChildLogger());

            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result));
        }

        /// <summary>
        /// Exports an environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A cloud environment.</returns>
        [HttpPost("{environmentId}/export")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(ExportAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status503ServiceUnavailable)]
        [HttpOperationalScope("export")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> ExportAsync(
            [FromRoute] Guid environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));

            if (FrontEndAppSettings.EnableExporting)
            {
                ExportCloudEnvironmentParameters exportEnvParams = await GetExportCloudEnvironmentParametersAsync();
                var result = await EnvironmentManager.ExportAsync(
                    environmentId,
                    exportEnvParams,
                    logger.NewChildLogger());

                var temp = Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(result);

                return Ok(temp);
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Create a new cloud environment.
        /// </summary>
        /// <param name="createEnvironmentInput">The cloud environment input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPost]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(CreateAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status307TemporaryRedirect)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(MessageCodes), StatusCodes.Status503ServiceUnavailable)]
        [HttpOperationalScope("create")]
        [Audit(AuditEventCategory.ResourceManagement)]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        [QuotaHeader]
        public async Task<IActionResult> CreateAsync(
            [FromBody] CreateCloudEnvironmentBody createEnvironmentInput,
            [FromServices] IDiagnosticsLogger logger)
        {
            // Apply values if we're authenticating using a GitHub token.
            GitHubFixedPlansMapper.ApplyValuesWhenGitHubTokenIsUsed(createEnvironmentInput, this.Request);

            var environmentCreateDetails = Mapper.Map<CreateCloudEnvironmentBody, EnvironmentCreateDetails>(createEnvironmentInput);
            logger.AddSkuName(environmentCreateDetails.SkuName);

            IsSecretQuotaReached(createEnvironmentInput.Secrets);

            // Build metrics manager
            var metricsInfo = await GetMetricsInfoAsync(logger);

            // Get start environment parameters
            StartCloudEnvironmentParameters startEnvironmentParams = await GetStartCloudEnvironmentParametersAsync();
            startEnvironmentParams.Secrets = environmentCreateDetails.Secrets;

            // Create environment
            var cloudEnvironment = await EnvironmentManager.CreateAsync(
                environmentCreateDetails, startEnvironmentParams, metricsInfo, logger.NewChildLogger());

            // Update audit id
            AuditAttribute.SetTargetResourceId(HttpContext, cloudEnvironment.Id);

            // Workout 201 location
            var location = new UriBuilder(Request.GetDisplayUrl());
            location.Path = $"{location.Path.TrimEnd('/')}/{cloudEnvironment.Id}";

            return Created(location.Uri, Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(cloudEnvironment));
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
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> DeleteAsync(
            [FromRoute] Guid environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(environmentId, nameof(environmentId));
            await ValidateEnvironmentIsNotSoftDeleted(environmentId, logger);

            await EnvironmentManager.SoftDeleteAsync(environmentId, logger.NewChildLogger());

            return NoContent();
        }

        /// <summary>
        /// Restore a deleted cloud environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Status code <see cref="HttpStatusCode.NoContent"/> if deleted, otherwise <see cref="HttpStatusCode.NotFound"/>.</returns>
        [HttpPatch("{environmentId}/restore")]
        [ThrottlePerUserHigh(nameof(EnvironmentsController), nameof(RestoreAsync))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("restore")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public IActionResult RestoreAsync(
            [FromRoute] string environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            // The restore feature is complete and tested. However, we want to resolve the billing questions
            // before making it public. When we are ready to make the restore feature available to users
            // we will move the code from the private "RestoreEnvironmentAsync" method to this method.
            // It is intentionally not behind feature flag as billing will require code changes.
            return Unauthorized();
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
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> UpdateAsync(
            [FromRoute] string environmentId,
            [FromBody] EnvironmentRegistrationCallbackBody callbackBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(callbackBody, nameof(callbackBody));
            ValidationUtil.IsRequired(callbackBody?.Payload, nameof(callbackBody.Payload));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionId, nameof(callbackBody.Payload.SessionId));
            ValidationUtil.IsRequired(callbackBody?.Payload?.SessionPath, nameof(callbackBody.Payload.SessionPath));

            var environment = await GetEnvironmentAsync(
                environmentId,
                validateSoftDeletedEnvironment: true,
                normalizeEnvironmentState: true,
                logger);

            var currentUserIdSet = CurrentUserProvider.CurrentUserIdSet;
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
        [ProducesResponseType(typeof(IEnumerable<MessageCodes>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [HttpOperationalScope("update_settings")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> UpdateSettingsAsync(
            [FromRoute] string environmentId,
            [FromBody] UpdateCloudEnvironmentBody updateEnvironmentInput,
            [FromServices] IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(updateEnvironmentInput, nameof(updateEnvironmentInput));
            ValidationUtil.IsRequired(logger, nameof(logger));

            var environment = await GetEnvironmentAsync(
                environmentId,
                validateSoftDeletedEnvironment: true,
                normalizeEnvironmentState: true,
                logger);

            EnvironmentAccessManager.AuthorizeEnvironmentAccess(environment, nonOwnerScopes: null, logger);

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            VsoPlan plan = null;
            VsoClaimsIdentity planAccessIdentity = null;
            Subscription subscription = null;
            if (!string.IsNullOrEmpty(updateEnvironmentInput.PlanId))
            {
                ValidationUtil.IsTrue(
                    VsoPlanInfo.TryParse(updateEnvironmentInput.PlanId, out var planInfo),
                    $"Invalid plan ID: {updateEnvironmentInput.PlanId}");

                // Validate the plan exists (and lookup the plan details).
                plan = await PlanManager.GetAsync(planInfo, logger);
                ValidationUtil.IsTrue(
                    plan != null, $"Plan {updateEnvironmentInput.PlanId} not found.");

                var accessToken = updateEnvironmentInput.PlanAccessToken;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    try
                    {
                        var principal = AccessTokenReader.ReadTokenPrincipal(accessToken, logger);
                        planAccessIdentity = new VsoClaimsIdentity(principal.Identities.Single());
                    }
                    catch (SecurityTokenException)
                    {
                        // The provided plan access token was not valid.
                        // (Exception details were logged by the token reader.)
                        return new UnauthorizedResult();
                    }
                }

                // Check if the user has access to write to the new plan.
                var requiredScopes = new[]
                {
                    PlanAccessTokenScopes.WriteEnvironments,
                    PlanAccessTokenScopes.WriteCodespaces,
                };
                EnvironmentAccessManager.AuthorizePlanAccess(plan, requiredScopes, planAccessIdentity, logger);

                subscription = await SubscriptionManager.GetSubscriptionAsync(planInfo.Subscription, logger.NewChildLogger());
                if (!await SubscriptionManager.CanSubscriptionCreatePlansAndEnvironmentsAsync(subscription, logger.NewChildLogger()))
                {
                    var message = $"{HttpStatusCode.Forbidden}: The subscription is not in a valid state.";
                    logger.AddSubscriptionId(planInfo.Subscription);
                    logger.AddReason(message);
                    return StatusCode(StatusCodes.Status403Forbidden, MessageCodes.SubscriptionCannotPerformAction);
                }

                if (string.IsNullOrEmpty(plan.Tenant))
                {
                    // Some older plans might not have their tenant ID property set, but the EnvironmentManager
                    // update requires it. The caller's tenant ID should be the same anyway.
                    plan.Tenant = CurrentUserProvider.Identity?.GetTenantId();
                }
            }

            var updateRequest = Mapper.Map<CloudEnvironmentUpdate>(updateEnvironmentInput);
            updateRequest.Plan = plan;
            updateRequest.PlanAccessIdentity = planAccessIdentity;

            var result = await EnvironmentManager.UpdateSettingsAsync(environment, updateRequest, subscription, logger);

            if (result.IsSuccess)
            {
                return Ok(Mapper.Map<CloudEnvironmentResult>(result.CloudEnvironment));
            }
            else if (result.ValidationErrors?.Contains(MessageCodes.EnvironmentNameAlreadyExists) == true)
            {
                return Conflict(result.ValidationErrors);
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
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> GetAvailableSettingsUpdatesAsync(
            [FromRoute] string environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            var environment = await GetEnvironmentAsync(
                environmentId,
                validateSoftDeletedEnvironment: true,
                normalizeEnvironmentState: true,
                logger);

            EnvironmentAccessManager.AuthorizeEnvironmentAccess(environment, nonOwnerScopes: null, logger);

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
                var plan = CurrentUserProvider.Identity.AuthorizedPlan;
                var planInfo = VsoPlanInfo.TryParse(plan);
                var visibleSkus = new List<ICloudEnvironmentSku>();

                var currentUserProfile = await CurrentUserProvider?.GetProfileAsync();
                foreach (var sku in availableUpdates.AllowedSkus)
                {
                    var isVisible = await SkuUtils.IsVisible(sku, planInfo, currentUserProfile);
                    if (isVisible)
                    {
                        visibleSkus.Add(sku);
                    }
                }

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

        /// <summary>
        /// Puts list of recent folders into environment record.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="folderPathInput">The new environment settings.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result containing the <see cref="CloudEnvironmentResult"/>.</returns>
        [HttpPatch("{environmentId}/folder")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(GetAvailableSettingsUpdatesAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IEnumerable<MessageCodes>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("update_MRU_folders")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> UpdateRecentFoldersListAsync(
            [FromRoute] string environmentId,
            [FromBody] CloudEnvironmentFolderBody folderPathInput,
            [FromServices] IDiagnosticsLogger logger)
        {
            ValidationUtil.IsRequired(folderPathInput, nameof(folderPathInput));
            ValidationUtil.IsRequired(logger, nameof(logger));

            var environment = await GetEnvironmentAsync(
                environmentId,
                validateSoftDeletedEnvironment: true,
                normalizeEnvironmentState: true,
                logger);

            EnvironmentAccessManager.AuthorizeEnvironmentAccess(environment, nonOwnerScopes: null, logger);

            // Reroute to correct location if needed
            var owningStamp = ControlPlaneInfo.GetOwningControlPlaneStamp(environment.Location);
            if (owningStamp.Location != CurrentLocationProvider.CurrentLocation)
            {
                return RedirectToLocation(owningStamp);
            }

            var result = await EnvironmentManager.UpdateFoldersListAsync(environment, folderPathInput, logger);
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
        /// Generates a heartbeat token for the specified environment.
        /// </summary>
        /// <param name="environmentId">The environment id.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>An object result that contains the token.</returns>
        [HttpGet("{environmentId}/heartbeattoken")]
        [ThrottlePerUserLow(nameof(EnvironmentsController), nameof(GenerateHeartBeatTokenAsync))]
        [ProducesResponseType(typeof(CloudEnvironmentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("heartbeat_tokens")]
        [Audit(AuditEventCategory.ResourceManagement, "environmentId")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> GenerateHeartBeatTokenAsync(
            [FromRoute] string environmentId,
            [FromServices] IDiagnosticsLogger logger)
        {
            var environment = await GetEnvironmentAsync(
                environmentId,
                validateSoftDeletedEnvironment: false,
                normalizeEnvironmentState: false,
                logger);

            EnvironmentAccessManager.AuthorizeEnvironmentAccess(environment, nonOwnerScopes: null, logger);

            if (environment.Type != EnvironmentType.StaticEnvironment)
            {
                return new ForbidResult();
            }

            var result = new HeartbeatTokenResult()
            {
                HeartbeatToken = await TokenProvider.GenerateVmTokenAsync(environmentId, logger),
            };

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
            bool validateSoftDeletedEnvironment,
            bool normalizeEnvironmentState,
            IDiagnosticsLogger logger)
        {
            logger.AddEnvironmentId(environmentId);
            ValidationUtil.IsRequired(environmentId, nameof(environmentId));

            var environment = await EnvironmentManager.GetAsync(Guid.Parse(environmentId), logger);

            if (validateSoftDeletedEnvironment)
            {
                await ValidateEnvironmentIsNotSoftDeleted(environment, logger);
            }

            // Normalize state
            if (normalizeEnvironmentState == true)
            {
                environment = await EnvironmentStateManager.NormalizeEnvironmentStateAsync(environment, logger.NewChildLogger());
            }

            return environment;
        }

        private async Task<MetricsInfo> GetMetricsInfoAsync(IDiagnosticsLogger logger)
        {
            try
            {
                var metricsClientInfo = await MetricsManager.GetMetricsInfoForRequestAsync(Request.Headers, logger.NewChildLogger());
                return new MetricsInfo
                {
                    IsoCountryCode = metricsClientInfo.IsoCountryCode,
                    AzureGeography = metricsClientInfo.AzureGeography,
                    VsoClientType = metricsClientInfo.VsoClientType,
                };
            }
            catch
            {
                // Ignore metrics exceptions here. Do not fail to create an environment due to missing metrics info.
                return null;
            }
        }

        // TODO t-aibha: refactor GetStartCLoudEnviornmentParamaters and GetExportCloudEnvironmentParameters once Ed has merged his changes for git export operation.
        private async Task<StartCloudEnvironmentParameters> GetStartCloudEnvironmentParametersAsync()
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

            var currentUserProfile = await CurrentUserProvider?.GetProfileAsync();

            return new StartCloudEnvironmentParameters
            {
                UserProfile = currentUserProfile,
                FrontEndServiceUri = serviceUri,
                ConnectionServiceUri = new Uri(FrontEndAppSettings.VSLiveShareApiEndpoint, UriKind.Absolute),
                CallbackUriFormat = callbackUriFormat,
                UserAuthToken = CurrentUserProvider.BearerToken,
                CurrentUserIdSet = CurrentUserProvider.CurrentUserIdSet,
            };
        }

        private async Task<ExportCloudEnvironmentParameters> GetExportCloudEnvironmentParametersAsync()
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

            var currentUserProfile = await CurrentUserProvider?.GetProfileAsync();

            return new ExportCloudEnvironmentParameters
            {
                UserProfile = currentUserProfile,
                FrontEndServiceUri = serviceUri,
                CallbackUriFormat = callbackUriFormat,
                UserAuthToken = CurrentUserProvider.BearerToken,
                CurrentUserIdSet = CurrentUserProvider.CurrentUserIdSet,
            };
        }

        private async Task<IActionResult> RestoreEnvironmentAsync(Guid environmentId, IDiagnosticsLogger logger)
        {
            var cloudEnvironment = await EnvironmentManager.DeleteRestoreAsync(environmentId, logger.NewChildLogger());
            return Ok(Mapper.Map<CloudEnvironment, CloudEnvironmentResult>(cloudEnvironment));
        }

        private async Task ValidateEnvironmentIsNotSoftDeleted(Guid environmentId, IDiagnosticsLogger logger)
        {
            await GetEnvironmentAsync(
                environmentId.ToString(),
                validateSoftDeletedEnvironment: true,
                normalizeEnvironmentState: false,
                logger);
        }

        private async Task ValidateEnvironmentIsNotSoftDeleted(CloudEnvironment environment, IDiagnosticsLogger logger)
        {
            if (environment == null)
            {
                return;
            }

            if (environment.IsDeleted == true)
            {
                var environmentSoftDeleteEnabled = await FrontEndAppSettings.EnvironmentManagerSettings.EnvironmentSoftDeleteEnabled(logger.NewChildLogger());

                if (environmentSoftDeleteEnabled == true)
                {
                    throw new EntityNotFoundException((int)MessageCodes.EnvironmentDoesNotExist);
                }
            }
        }

        private void IsSecretQuotaReached(IEnumerable<SecretDataBody> secrets)
        {
            if (secrets != null && secrets.Count(x => x.Type == SecretType.EnvironmentVariable) > MaxEnvironmentVariablesSecrets)
            {
                throw new ForbiddenException(
                            (int)MessageCodes.ExceededSecretsQuota,
                            message: $"Quota reached for the secrets type '{SecretType.EnvironmentVariable}'");
            }
        }
    }
}
