// <copyright file="ISharedIdentitiesProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Client for managing shared identities for CMK.
    /// </summary>
    public interface ISharedIdentitiesProvider
    {
        /// <summary>
        /// Attaches a managed identity to a Storage Account.
        /// </summary>
        /// <param name="storageAccountResourceId">The storage account reference.</param>
        /// <param name="managedIdentity">A managed identity.</param>
        /// <param name="logger">Logger.</param>
        /// <returns>Task.</returns>
        Task AssignSharedIdentityAsync(string storageAccountResourceId, StorageSharedIdentity managedIdentity, IDiagnosticsLogger logger);

        /// <summary>
        /// Retrieves shared identity details from a storage account.
        /// </summary>
        /// <param name="storageAccountResourceId">A storage account resource ID.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Task.</returns>
        Task<StorageSharedIdentity> GetSharedIdentityAsync(string storageAccountResourceId, IDiagnosticsLogger logger, CancellationToken cancellationToken = default);
    }
}