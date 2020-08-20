// <copyright file="IArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts
{
    /// <summary>
    /// Manages system storage accounts used for archiving environment file storage.
    /// </summary>
    public interface IArchiveStorageProvider
    {
        /// <summary>
        /// Gets an archive storage account in the specified region having the minimum required available GB of storage.
        /// </summary>
        /// <param name="location">The azure location.</param>
        /// <param name="minimumRequiredGB">The minimum required GB of available storage.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <param name="forceCapacityCheck">A value indicating whether to re-evaluate storage capacity on any cached entities.</param>
        /// <returns>An <see cref="ISharedStorageInfo"/> instance.</returns>
        /// <remarks>
        /// The implementation should create a new storage account if none exists.
        /// The implementation should create a new storage account if existing ones don't have sufficient space.
        /// The storage account specifications should be as follows:
        ///     Account kind: General purpose v2 (StorageV2)
        ///     Replication: Zone-redundant (ZRS) [not available in every location!]
        ///     Performance/Access tier: Standard/Hot
        /// See https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-storage-tiers?tabs=azure-portal#cool-and-archive-early-deletion.
        /// </remarks>
        Task<ISharedStorageInfo> GetArchiveStorageAccountAsync(AzureLocation location, int minimumRequiredGB, IDiagnosticsLogger logger, bool forceCapacityCheck = false);

        /// <summary>
        /// Lists the archive storage accounts that exist in the specified azure location.
        /// </summary>
        /// <param name="location">The azure location.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>A list of <see cref="ISharedStorageInfo"/>.</returns>
        Task<IEnumerable<ISharedStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger);
    }
}
