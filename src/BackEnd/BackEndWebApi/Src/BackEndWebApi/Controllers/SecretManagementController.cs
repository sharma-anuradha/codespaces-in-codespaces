// <copyright file="SecretManagementController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using SecretFilterType = Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager.SecretFilterType;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApi.Controllers
{
    /// <summary>
    /// Secret management controller for the back end.
    /// </summary>
    [ApiController]
    [Route(SecretManagerHttpContract.ResourceSecretManagerV1Route)]
    [LoggingBaseName("backend_secret_management_controller")]
    public class SecretManagementController : ControllerBase, ISecretManagerHttpContract
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretManagementController"/> class.
        /// </summary>
        /// <param name="secretManager">Secret manager.</param>
        /// <param name="mapper">Mapper.</param>
        public SecretManagementController(ISecretManager secretManager, IMapper mapper)
        {
            SecretManager = secretManager;
            Mapper = mapper;
        }

        private ISecretManagerHttpContract SecretManagerHttp { get => this; }

        private ISecretManager SecretManager { get; }

        private IMapper Mapper { get; }

        /// <summary>
        /// Create a new secret under specified resource.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="createSecretBody">Create secret body.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpPost("{resourceId}/" + SecretManagerHttpContract.SecretManagementOperation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpOperationalScope("create")]
        public async Task<IActionResult> CreateSecretAsync(
            [FromRoute] Guid resourceId,
            [FromBody] CreateSecretBody createSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            Requires.NotEmpty(resourceId, nameof(resourceId));
            Requires.NotNull(createSecretBody, nameof(createSecretBody));
            Requires.NotNullOrEmpty(createSecretBody.SecretName, nameof(createSecretBody.SecretName));
            Requires.NotNullOrEmpty(createSecretBody.Value, nameof(createSecretBody.Value));

            SecretResult secretResult;
            try
            {
                secretResult = await SecretManagerHttp.CreateSecretAsync(resourceId, createSecretBody, logger.NewChildLogger());
            }
            catch (KeyVaultResourceNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }

            return Ok(secretResult);
        }

        /// <summary>
        /// Get all the secrets under specified resources.
        /// </summary>
        /// <param name="resourceIds">Resource Ids.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpOperationalScope("create")]
        public async Task<IActionResult> GetResourceSecrets(
            [FromQuery(Name = "resourceId")] IEnumerable<Guid> resourceIds,
            [FromServices] IDiagnosticsLogger logger)
        {
            IEnumerable<ResourceSecretsResult> resourceSecrets;
            try
            {
                Requires.NotNullOrEmpty(resourceIds, nameof(resourceIds));
                resourceSecrets = await SecretManagerHttp.GetSecretsAsync(resourceIds, logger.NewChildLogger());
            }
            catch (KeyVaultResourceNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }

            return Ok(resourceSecrets);
        }

        /// <inheritdoc/>
        Task ISecretManagerHttpContract.AddOrUpdateSecreFiltersAsync(
            Guid resourceId,
            Guid secretId,
            IDictionary<SecretFilterType, string> secretFilters,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<SecretResult> ISecretManagerHttpContract.CreateSecretAsync(
            Guid resourceId,
            CreateSecretBody createSecretBody,
            IDiagnosticsLogger logger)
        {
            logger.AddBaseResourceId(resourceId);
            var creatSecretInput = Mapper.Map<CreateSecretInput>(createSecretBody);
            var secretResult = await SecretManager.CreateSecretAsync(resourceId, creatSecretInput, logger);
            return Mapper.Map<SecretResult>(secretResult);
        }

        /// <inheritdoc/>
        Task ISecretManagerHttpContract.DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        Task ISecretManagerHttpContract.DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterType,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        async Task<IEnumerable<ResourceSecretsResult>> ISecretManagerHttpContract.GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger)
        {
            var resourceSecrets = await SecretManager.GetSecretsAsync(resourceIds, logger);
            return Mapper.Map<IEnumerable<ResourceSecretsResult>>(resourceSecrets);
        }

        /// <inheritdoc/>
        Task<SecretResult> ISecretManagerHttpContract.UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretBody updateSecretBody,
            IDiagnosticsLogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
