// <copyright file="IStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// An interface for Storage providers.
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Create a storage resource.
        /// </summary>
        /// <param name="input">Provides input to Create Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        /// Result of the Create operations which includes continuationToken which
        /// can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<FileShareProviderCreateResult> CreateAsync(FileShareProviderCreateInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete a storage resource.
        /// </summary>
        /// <param name="input">Provides input to Delete Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        ///  Result of the Delete operations which includes continuationToken which
        ///  can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<FileShareProviderDeleteResult> DeleteAsync(FileShareProviderDeleteInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Get information on the storage resource during assignment time.
        /// </summary>
        /// <param name="input">Provides input to get the assignment information for the Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        ///  Result of the Assign operation which includes continuationToken which
        ///  can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<FileShareProviderAssignResult> AssignAsync(FileShareProviderAssignInput input, IDiagnosticsLogger logger);

        /// <summary>
        /// Suspend a storage resource.
        /// </summary>
        /// <param name="input">Provides input to get the suspend information for the Azure file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>
        ///  Result of the suspend operation which includes continuationToken which
        ///  can be used to call this method again to continue the next step of operation.
        /// </returns>
        Task<FileShareProviderArchiveResult> ArchiveAsync(FileShareProviderArchiveInput input, IDiagnosticsLogger logger);
    }
}