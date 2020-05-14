// <copyright file="SecretsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Middleware;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

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
        public async Task<IActionResult> CreateSecretAsync(
            [FromQuery] string planId,
            [FromBody] ScopedCreateSecretBody scopedCreateSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(planId, nameof(planId));
            Requires.NotNull(scopedCreateSecretBody, nameof(scopedCreateSecretBody));

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
        [HttpOperationalScope("create")]
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
    }
}