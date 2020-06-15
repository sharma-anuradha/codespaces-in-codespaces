// <copyright file="IIdentityMapRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.IdentityMap
{
    /// <summary>
    /// A repository of <see cref="IIdentityMapEntity"/>.
    /// </summary>
    public interface IIdentityMapRepository : IDocumentDbCollection<IdentityMapEntity>
    {
        /// <summary>
        /// Get an <see cref="IIdentityMapEntity"/> record by MSA username.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The instance, or null.</returns>
        Task<IIdentityMapEntity> GetByUserNameAsync(string userName, string tenantId, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete an <see cref="IIdentityMapEntity"/> record.
        /// </summary>
        /// <param name="userName">The username.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>True if deleted.</returns>
        Task<bool> DeleteByUserNameAsync(string userName, string tenantId, IDiagnosticsLogger logger);

        /// <summary>
        /// Update an <see cref="IIdentityMapEntity"/> record with profile ids.
        /// </summary>
        /// <param name="map">The identity map.</param>
        /// <param name="canonicalUserId">The canonical user id, or null if not specified.</param>
        /// <param name="profileId">The Profile id, or null if not specified.</param>
        /// <param name="profileProviderId">The Profile provider id, or null if not specified.</param>
        /// <param name="linkedUserIds">Array of linked user IDs, or null if not specified.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The updated instance.</returns>
        Task<IIdentityMapEntity> BackgroundUpdateIfChangedAsync(
            IIdentityMapEntity map,
            string canonicalUserId,
            string profileId,
            string profileProviderId,
            string[] linkedUserIds,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Get an <see cref="IIdentityMapEntity"/> record by <see cref="UserIdSet"/>.
        /// </summary>
        /// <param name="userIdSet">The UserIdSet.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The instance, or null.</returns>
        Task<IIdentityMapEntity> GetByUserIdSetAsync(UserIdSet userIdSet, IDiagnosticsLogger logger);
    }
}
