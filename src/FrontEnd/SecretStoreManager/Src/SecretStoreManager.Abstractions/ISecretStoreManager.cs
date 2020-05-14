// <copyright file="ISecretStoreManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
    }
}
