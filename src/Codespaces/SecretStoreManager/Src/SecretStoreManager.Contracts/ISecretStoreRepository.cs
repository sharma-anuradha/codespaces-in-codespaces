// <copyright file="ISecretStoreRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SecretStoreManager
{
    /// <summary>
    /// Repository for <see cref="SecretStore"/>.
    /// </summary>
    public interface ISecretStoreRepository : IDocumentDbCollection<SecretStore>
    {
        /// <summary>
        /// Fetch the secret store document by owner and plan.
        /// </summary>
        /// <param name="secretScope">Secret scope.</param>
        /// <param name="ownerId">Owner Id.</param>
        /// <param name="planId">Plan Id.</param>
        /// <param name="logger">The Logger.</param>
        /// <returns>Matching <see cref="SecretStore"/> document.</returns>
        Task<SecretStore> GetSecretStoreByOwnerAndPlanAsync(
            SecretScope secretScope,
            string ownerId,
            string planId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch the list of secret store document that the user has access to, for the given plan.
        /// This will include user's personal secret store and the plan level shared store if any.
        /// </summary>
        /// <param name="userId">User Id.</param>
        /// <param name="planId">Plan Id.</param>
        /// <param name="logger">The Logger.</param>
        /// <returns>A list of matching <see cref="SecretStore"/> documents.</returns>
        Task<IEnumerable<SecretStore>> GetAllPlanSecretStoresByUserAsync(
            string userId,
            string planId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch the list of secret store documents that are associated with the given plan.
        /// </summary>
        /// <param name="planId">Plan Id.</param>
        /// <param name="logger">The Logger.</param>
        /// <returns>A list of matching <see cref="SecretStore"/> documents.</returns>
        Task<IEnumerable<SecretStore>> GetSecretStoresByPlanIdAsync(
            string planId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a secret store by it's Id and Plan Id (partition key).
        /// </summary>
        /// <param name="id">The secret store Id.</param>
        /// <param name="planId">The Plan Id of the secret store (which is the partition key).</param>
        /// <param name="logger">The Logger.</param>
        /// <returns>True if the document was deleted.</returns>
        Task<bool> DeleteAsync(string id, string planId, IDiagnosticsLogger logger);

        /// <summary>
        /// Get a secret store by it's Id and Plan Id (partition key).
        /// </summary>
        /// <param name="id">The secret store Id.</param>
        /// <param name="planId">The Plan Id of the secret store (which is the partition key).</param>
        /// <param name="logger">The Logger.</param>
        /// <returns>The secret store document.</returns>
        Task<SecretStore> GetAsync(string id, string planId, IDiagnosticsLogger logger);
    }
}
