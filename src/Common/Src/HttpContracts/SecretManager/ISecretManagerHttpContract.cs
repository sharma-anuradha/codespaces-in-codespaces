// <copyright file="ISecretManagerHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.SecretManager
{
    /// <summary>
    /// Secret Managener Http Contract.
    /// </summary>
    public interface ISecretManagerHttpContract
    {
        /// <summary>
        /// Get the set of secrets from the given list of resources.
        /// </summary>
        /// <param name="resourceIds">Resource ids.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Secrets from each of the given resources.</returns>
        Task<IEnumerable<ResourceSecretsResult>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create a new secret.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="createSecretBody">Create secret request body.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Created secret.</returns>
        Task<SecretResult> CreateSecretAsync(
            Guid resourceId,
            CreateSecretBody createSecretBody,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update a secret.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="updateSecretBody">Update secret request body.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Updated secret.</returns>
        Task<SecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretBody updateSecretBody,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a secret.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a secret filter.
        /// </summary>
        /// <param name="resourceId">Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="secretFilterType">Secret filter type.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Updated secret.</returns>
        Task<SecretResult> DeleteSecretFilterAsync(
            Guid resourceId,
            Guid secretId,
            SecretFilterType secretFilterType,
            IDiagnosticsLogger logger);
    }
}
