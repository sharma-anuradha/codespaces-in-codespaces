// <copyright file="IEntityRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.DocumentDB;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Actions
{
    /// <summary>
    /// A repository for use with the <see cref="IEntityAction"/> interfaces.
    /// </summary>
    /// <typeparam name="T">The type of entity contained in the repository.</typeparam>
    public interface IEntityRepository<T>
    {
        /// <summary>
        /// Get an entity from a repository.
        /// </summary>
        /// <param name="id">The entity id.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The entity.</returns>
        Task<T> GetAsync(DocumentDbKey id, IDiagnosticsLogger logger);

        /// <summary>
        /// Update the entity record in the repository.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>The updated entity reference.</returns>
        Task<T> UpdateAsync(T entity, IDiagnosticsLogger logger);
    }
}
