// <copyright file="IStorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

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

        /// <summary>
        /// Gets the stroage account key from the target account.
        /// </summary>
        /// <param name="storageAccount">Target storage account.</param>
        /// <returns>Account key.</returns>
        Task<string> GetStorageAccountKey(IStorageAccount storageAccount);

        /// <summary>
        /// Given a specific storage type, derive the mountable file name.
        /// </summary>
        /// <param name="storageType">Target stroage type.</param>
        /// <returns>Mountable File Name.</returns>
        string GetStorageMountableFileName(StorageType storageType);

        /// <summary>
        /// Gets the stroage mountable share name.
        /// </summary>
        /// <returns>Mountable Share Name.</returns>
        string GetStorageMountableShareName();
    }
}
