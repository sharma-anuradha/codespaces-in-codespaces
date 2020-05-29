// <copyright file="ISecretStoreManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager
{
    /// <summary>
    /// Secret store manager.
    /// </summary>
    public interface ISecretStoreManager
    {
        /// <summary>
        /// Fetch all the secrets that the user has access to, for the given plan.
        /// This will include user's personal secrets and the plan level shared secrets if any.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Scoped secrets.</returns>
        Task<IEnumerable<ScopedSecretResult>> GetAllSecretsByPlanAsync(
            string planId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch all the secret stores that the user has access to, for the given plan.
        /// This will include user's personal secret store and the plan level shared secret store if any.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Scoped secrets.</returns>
        Task<IEnumerable<SecretStore>> GetAllSecretStoresByPlanAsync(
            string planId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create a new secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="scopedCreateSecretInput">Scoped create secret input.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Created secret.</returns>
        Task<ScopedSecretResult> CreateSecretAsync(
            string planId,
            ScopedCreateSecretInput scopedCreateSecretInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Update an existing secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="scopedUpdateSecretInput">Scoped update secret input.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Updated secret.</returns>
        Task<ScopedSecretResult> UpdateSecretAsync(
            string planId,
            Guid secretId,
            ScopedUpdateSecretInput scopedUpdateSecretInput,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete an existing secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="secretScope">The secret scope.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DeleteSecretAsync(
            string planId,
            Guid secretId,
            SecretScope secretScope,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a filter on an existing secret.
        /// </summary>
        /// <param name="planId">The plan id.</param>
        /// <param name="secretId">The secret id.</param>
        /// <param name="secretFilterType">The secret filter type.</param>
        /// <param name="secretScope">The secret scope.</param>
        /// <param name="logger">IDiagnostics Logger.</param>
        /// <returns>Updated secret.</returns>
        Task<ScopedSecretResult> DeleteSecretFilterAsync(
            string planId,
            Guid secretId,
            SecretFilterType secretFilterType,
            SecretScope secretScope,
            IDiagnosticsLogger logger);
    }
}
