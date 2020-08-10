// <copyright file="SecretsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.AspNetCore.Diagnostics;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Constants;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using SecretScope = Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts.SecretScope;
using SecretScopeHttpContract = Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager.SecretScope;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Controllers
{
    /// <summary>
    /// Secret stores controller.
    /// </summary>
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerUtility.UserAuthenticationSchemes)]
    [FriendlyExceptionFilter]
    [Route(ServiceConstants.ApiV1Route)]
    [LoggingBaseName("secretstores_controller")]
    public class SecretsController : ControllerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsController"/> class.
        /// </summary>
        /// <param name="secretStoreManager">The secret store manager.</param>
        /// <param name="planManager">The plan manager.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="controlPlaneInfo">The control plane info instance.</param>
        /// <param name="mapper">Automapper.</param>
        public SecretsController(
            ISecretStoreManager secretStoreManager,
            IPlanManager planManager,
            ICurrentUserProvider currentUserProvider,
            IControlPlaneInfo controlPlaneInfo,
            IMapper mapper)
        {
            SecretStoreManager = secretStoreManager;
            PlanManager = planManager;
            CurrentUserProvider = currentUserProvider;
            ControlPlaneInfo = controlPlaneInfo;
            Mapper = mapper;
        }

        private ISecretStoreManager SecretStoreManager { get; }

        private IPlanManager PlanManager { get; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IMapper Mapper { get; }

        /// <summary>
        /// Create a secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="scopedCreateSecretBody">Scoped create secret body.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpPost]
        [ThrottlePerUserLow(nameof(SecretsController), nameof(CreateSecretAsync))]
        [ProducesResponseType(typeof(ScopedSecretResultBody), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("create")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> CreateSecretAsync(
            [FromQuery] string planId,
            [FromBody] ScopedCreateSecretBody scopedCreateSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));
            Requires.NotNull(scopedCreateSecretBody, nameof(scopedCreateSecretBody));
            CheckForDuplicateFilters(scopedCreateSecretBody.Filters);

            var scopedCreateSecretInput = Mapper.Map<ScopedCreateSecretInput>(scopedCreateSecretBody);

            var scopedSecretResult = await SecretStoreManager.CreateSecretAsync(
                planId,
                scopedCreateSecretInput,
                logger.NewChildLogger());

            return Ok(Mapper.Map<ScopedSecretResultBody>(scopedSecretResult));
        }

        /// <summary>
        /// Fetch all the secrets that the user has access to, for the given plan.
        /// This will include user's personal secrets and the plan level shared secrets if any.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpGet]
        [ThrottlePerUserLow(nameof(SecretsController), nameof(GetSecretStoresByPlan))]
        [ProducesResponseType(typeof(IEnumerable<ScopedSecretResultBody>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("get")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> GetSecretStoresByPlan(
            [FromQuery] string planId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));

            var allScopedSecrets = await SecretStoreManager.GetAllSecretsByPlanAsync(
                planId,
                logger.NewChildLogger());

            return Ok(Mapper.Map<IEnumerable<ScopedSecretResultBody>>(allScopedSecrets));
        }

        /// <summary>
        /// Update a secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="scopedUpdateSecretBody">Scoped update secret body.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpPut("{secretId}")]
        [ThrottlePerUserLow(nameof(SecretsController), nameof(CreateSecretAsync))]
        [ProducesResponseType(typeof(ScopedSecretResultBody), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("update")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> UpdateSecretAsync(
            [FromQuery] string planId,
            [FromRoute] Guid secretId,
            [FromBody] ScopedUpdateSecretBody scopedUpdateSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));
            Requires.NotEmpty(secretId, nameof(secretId));
            Requires.NotNull(scopedUpdateSecretBody, nameof(scopedUpdateSecretBody));
            CheckForDuplicateFilters(scopedUpdateSecretBody.Filters);

            var scopedUpdateSecretInput = Mapper.Map<ScopedUpdateSecretInput>(scopedUpdateSecretBody);

            var scopedSecretResult = await SecretStoreManager.UpdateSecretAsync(
                planId,
                secretId,
                scopedUpdateSecretInput,
                logger.NewChildLogger());

            return Ok(Mapper.Map<ScopedSecretResultBody>(scopedSecretResult));
        }

        /// <summary>
        /// Delete a secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="scope">The secret scope.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpDelete("{secretId}")]
        [ThrottlePerUserLow(nameof(SecretsController), nameof(CreateSecretAsync))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpOperationalScope("delete")]
        [MdmMetric(name: MdmMetricConstants.ControlPlaneLatency, metricNamespace: MdmMetricConstants.CodespacesHealthNamespace)]
        public async Task<IActionResult> DeleteSecretAsync(
            [FromQuery] string planId,
            [FromQuery] SecretScopeHttpContract scope,
            [FromRoute] Guid secretId,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));
            Requires.NotEmpty(secretId, nameof(secretId));

            await SecretStoreManager.DeleteSecretAsync(
                planId,
                secretId,
                Mapper.Map<SecretScope>(scope),
                logger.NewChildLogger());

            return NoContent();
        }

        private void CheckForDuplicateFilters(IEnumerable<SecretFilterBody> filters)
        {
            if (filters != null && filters.Any())
            {
                // Where there exists anotherFilter with same type as the current filter, then the filters collection has duplicates.
                var hasDuplicates = filters.Where(filter => filters.Any(anotherFilter => filter != anotherFilter && filter.Type == anotherFilter.Type)).Any();
                ValidationUtil.IsTrue(!hasDuplicates, "Duplicate filters are not allowed");
            }
        }
    }
}