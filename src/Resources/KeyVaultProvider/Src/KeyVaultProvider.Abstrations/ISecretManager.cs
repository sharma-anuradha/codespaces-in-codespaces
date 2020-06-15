// <copyright file="ISecretManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider
{
    /// <summary>
    /// Secret manager.
    /// </summary>
    public interface ISecretManager
    {
        /// <summary>
        /// Get the set of secrets from the given list of key vaults.
        /// </summary>
        /// <param name="resourceIds">Key vault resource ids.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Secrets from the given key vaults.</returns>
        Task<IEnumerable<ResourceSecrets>> GetSecretsAsync(
            IEnumerable<Guid> resourceIds,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create a new secret.
        /// </summary>
        /// <param name="resourceId">Key vault Resource Id.</param>
        /// <param name="createSecretInput">New secret.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Created secret information.</returns>
        Task<UserSecretResult> CreateSecretAsync(
            Guid resourceId,
            CreateSecretInput createSecretInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update a secret.
        /// </summary>
        /// <param name="resourceId">Key vault Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="updateSecretInput">Secret to update.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Updated secret information.</returns>
        Task<UserSecretResult> UpdateSecretAsync(
            Guid resourceId,
            Guid secretId,
            UpdateSecretInput updateSecretInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a secret.
        /// </summary>
        /// <param name="resourceId">Key vault Resource Id.</param>
        /// <param name="secretId">Secret Id.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteSecretAsync(
            Guid resourceId,
            Guid secretId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get applicable secrets' data after applying the secret filters.
        /// </summary>
        /// <param name="filterSecretsInput">Input to calculate applicable secrets.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Unique collection of <see cref="UserSecretData"/>.</returns>
        Task<IEnumerable<UserSecretData>> GetApplicableSecretsAndValuesAsync(
            FilterSecretsInput filterSecretsInput,
            IDiagnosticsLogger logger);
    }
}
