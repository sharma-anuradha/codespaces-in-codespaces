// <copyright file="IArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <summary>
    /// Manages system storage accounts used for archiving environment file storage.
    /// </summary>
    public interface IArchiveStorageProvider
    {
        /// <summary>
        /// Gets an archive storage account in the specified region having the minimum required available GB of storage.
        /// </summary>
        /// <param name="azureLocation">The azure location.</param>
        /// <param name="minimumRequiredGB">The minimum required GB of available storage.</param>
        /// <returns>An <see cref="IArchiveStorageInfo"/> instance.</returns>
        /// <remarks>
        /// The implementation should create a new storage account if none exists.
        /// The implementation should create a new storage account if existing ones don't have sufficient space.
        /// The storage account specifications should be as follows:
        ///     Account kind: General purpose v2 (StorageV2)
        ///     Replication: Zone-redundant (ZRS) [not available in every location!]
        ///     Performance/Access tier: Standard/Hot
        /// See https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-storage-tiers?tabs=azure-portal#cool-and-archive-early-deletion.
        /// </remarks>
        Task<IArchiveStorageInfo> GetArchiveStorageAccountAsync(AzureLocation azureLocation, int minimumRequiredGB);

        /// <summary>
        /// Lists the archive storage accounts that exist in the specified azure location.
        /// </summary>
        /// <param name="azureLocation">The azure location.</param>
        /// <returns>A list of <see cref="IArchiveStorageInfo"/>.</returns>
        Task<IEnumerable<IArchiveStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation azureLocation);
    }
}
