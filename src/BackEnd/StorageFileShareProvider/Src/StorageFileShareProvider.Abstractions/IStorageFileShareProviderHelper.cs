// <copyright file="IStorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// Represents helper operations required by the <see cref="StorageFileShareProvider"/> class.
    /// </summary>
    public interface IStorageFileShareProviderHelper
    {
        /// <summary>
        /// Create an Azure Storage account.
        /// </summary>
        /// <param name="azureSubscriptionId">Azure subscription id to create storage account in.</param>
        /// <param name="azureRegion">Azure region to create storage account in.</param>
        /// <param name="azureResourceGroup">Azure resource group to create storage account in.</param>
        /// <param name="azureSkuName">Azure Sku name for the storage account.</param>
        /// <param name="resourceTags">Azure tags to attach to the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The azure resource info of the storage account.</returns>
        Task<AzureResourceInfo> CreateStorageAccountAsync(string azureSubscriptionId, string azureRegion, string azureResourceGroup, string azureSkuName, IDictionary<string, string> resourceTags, IDiagnosticsLogger logger);

        /// <summary>
        /// Create a file share in the provided storage account.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task CreateFileShareAsync(AzureResourceInfo azureStorageAccountId, IDiagnosticsLogger logger);

        /// <summary>
        /// Prepare the file share by seeding it with the blob specified.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageCopyItems">Array of storage items to copy.</param>
        /// <param name="storageSizeInGb">Azure storage size in GB.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The task info that can be used to query the task.</returns>
        Task<PrepareFileShareTaskInfo> StartPrepareFileShareAsync(AzureResourceInfo azureResourceInfo, IEnumerable<StorageCopyItem> storageCopyItems, int storageSizeInGb, IDiagnosticsLogger logger);

        /// <summary>
        /// Check if the preparation of the file share has completed.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="prepareTaskInfo">The info for the task preparing the file share.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Status.</returns>
        Task<PrepareFileShareStatus> CheckPrepareFileShareAsync(AzureResourceInfo azureResourceInfo, PrepareFileShareTaskInfo prepareTaskInfo, IDiagnosticsLogger logger);

        /// <summary>
        /// Provides the connection information needed to connect to the file share.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageType">The type of storage to get conneciton info for.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The connection information for the share.</returns>
        Task<ShareConnectionInfo> GetConnectionInfoAsync(AzureResourceInfo azureResourceInfo, StorageType storageType, IDiagnosticsLogger logger);

        /// <summary>
        /// Delete the storage account.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task DeleteStorageAccountAsync(AzureResourceInfo azureResourceInfo, IDiagnosticsLogger logger);
    }
}
