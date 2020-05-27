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
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("create")]
        public async Task<IActionResult> CreateSecretAsync(
            [FromRoute] Guid resourceId,
            [FromBody] CreateSecretBody createSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotNull(createSecretBody, nameof(createSecretBody));
                Requires.NotNullOrEmpty(createSecretBody.SecretName, nameof(createSecretBody.SecretName));
                Requires.NotNullOrEmpty(createSecretBody.Value, nameof(createSecretBody.Value));

                var secretResult = await SecretManagerHttp.CreateSecretAsync(resourceId, createSecretBody, logger.NewChildLogger());
                return Ok(secretResult);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("get")]
        public async Task<IActionResult> GetResourceSecretsAsync(
            [FromQuery(Name = "resourceId")] IEnumerable<Guid> resourceIds,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotNullOrEmpty(resourceIds, nameof(resourceIds));
                var resourceSecrets = await SecretManagerHttp.GetSecretsAsync(resourceIds, logger.NewChildLogger());
                return Ok(resourceSecrets);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Update an existing secret under specified resource.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="updateSecretBody">Update secret body.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpPut("{resourceId}/" + SecretManagerHttpContract.SecretManagementOperation + "/{secretId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("update")]
        public async Task<IActionResult> UpdateSecretAsync(
            [FromRoute] Guid resourceId,
            [FromRoute] Guid secretId,
            [FromBody] UpdateSecretBody updateSecretBody,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotEmpty(secretId, nameof(secretId));
                Requires.NotNull(updateSecretBody, nameof(updateSecretBody));

                var secretResult = await SecretManagerHttp.UpdateSecretAsync(resourceId, secretId, updateSecretBody, logger.NewChildLogger());
                return Ok(secretResult);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Delete a secret under a specified resource.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpDelete("{resourceId}/" + SecretManagerHttpContract.SecretManagementOperation + "/{secretId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("delete")]
        public async Task<IActionResult> DeleteSecretAsync(
            [FromRoute] Guid resourceId,
            [FromRoute] Guid secretId,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotEmpty(secretId, nameof(secretId));

                await SecretManagerHttp.DeleteSecretAsync(resourceId, secretId, logger.NewChildLogger());
                return NoContent();
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Delete a filter from a secret under a specified resource.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="secretFilterType">Secret filter type.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The IActionResult.</returns>
        [HttpDelete("{resourceId}/"
                    + SecretManagerHttpContract.SecretManagementOperation
                    + "/{secretId}/"
                    + SecretManagerHttpContract.FilterManagementOperation
                    + "/{secretFilterType}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpOperationalScope("delete_filter")]
        public async Task<IActionResult> DeleteSecretFilterAsync(
            [FromRoute] Guid resourceId,
            [FromRoute] Guid secretId,
            [FromRoute] SecretFilterType secretFilterType,
            [FromServices] IDiagnosticsLogger logger)
        {
            try
            {
                Requires.NotEmpty(resourceId, nameof(resourceId));
                Requires.NotEmpty(secretId, nameof(secretId));

                var secretResult = await SecretManagerHttp.DeleteSecretFilterAsync(resourceId, secretId, secretFilterType, logger.NewChildLogger());
                return Ok(secretResult);
            }
            catch (NotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException)
            {
                return BadRequest();
            }
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
        async Task ISecretManagerHttpContract.DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger)
        {
            logger.AddBaseResourceId(resourceId);
            await SecretManager.DeleteSecretAsync(resourceId, secretId, logger);
        }

        /// <inheritdoc/>
        async Task<SecretResult> ISecretManagerHttpContract.DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterTypeContract,
            IDiagnosticsLogger logger)
        {
            logger.AddBaseResourceId(resourceId);
            var secretFilterType = Mapper.Map<KeyVaultProvider.Models.SecretFilterType>(secretFilterTypeContract);
            var secretResult = await SecretManager.DeleteSecretFilterAsync(resourceId, secretId, secretFilterType, logger);
            return Mapper.Map<SecretResult>(secretResult);
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
        async Task<SecretResult> ISecretManagerHttpContract.UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretBody updateSecretBody,
            IDiagnosticsLogger logger)
        {
            logger.AddBaseResourceId(resourceId);
            var updateSecretInput = Mapper.Map<UpdateSecretInput>(updateSecretBody);
            var secretResult = await SecretManager.UpdateSecretAsync(resourceId, secretId, updateSecretInput, logger);
            return Mapper.Map<SecretResult>(secretResult);
        }
    }
}
