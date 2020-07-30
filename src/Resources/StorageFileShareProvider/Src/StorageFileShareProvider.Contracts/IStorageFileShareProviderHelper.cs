// <copyright file="IStorageFileShareProviderHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.File;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
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
        Task<AzureResourceInfo> CreateStorageAccountAsync(
            string azureSubscriptionId,
            string azureRegion,
            string azureResourceGroup,
            string azureSkuName,
            IDictionary<string, string> resourceTags,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Enables KeyVault Encryption on a Storage Account.
        /// </summary>
        /// <param name="azureResourceInfo">Storage account reference.</param>
        /// <param name="keyVaultUri">The KeyVault URI.</param>
        /// <param name="keyName">The name of the key within the Vault.</param>
        /// <param name="keyVersion">The key version. Can be empty string.</param>
        /// <param name="logger">A logger instance.</param>
        /// <returns>Task.</returns>
        Task EnableKeyVaultEncryptionAsync(
            AzureResourceInfo azureResourceInfo,
            string keyVaultUri,
            string keyName,
            string keyVersion,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Create a file share in the provided storage account.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task CreateFileShareAsync(
            AzureResourceInfo azureStorageAccountId,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Provides the connection information needed to connect to the file share.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageType">The type of storage to get conneciton info for.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>The connection information for the share.</returns>
        Task<ShareConnectionInfo> GetConnectionInfoAsync(
            AzureResourceInfo azureResourceInfo,
            StorageType storageType,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete the storage account.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task DeleteStorageAccountAsync(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete target blob.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageAccountKey">Optional storage account key.</param>
        /// <param name="blobContainerName">Target blob container name.</param>
        /// <param name="blobName">Target blob name if known.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task DeleteBlobAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            string blobName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Delete target blob container (and any blobs inside).
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageAccountKey">Optional storage account key.</param>
        /// <param name="blobContainerName">Target blob container name.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Task.</returns>
        Task DeleteBlobContainerAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the underlying azure storage account for a given resource.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Target storage account.</returns>
        Task<IStorageAccount> GetStorageAccount(
            AzureResourceInfo azureResourceInfo,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the underlying azure cloud storage account for a given resource.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info of the storage account.</param>
        /// <param name="storageAccountKey">Optional storage account key.</param>
        /// <param name="logger">The diagnostics logger.</param>
        /// <returns>Target cloud storage account.</returns>
        Task<CloudStorageAccount> GetCloudStorageAccount(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the stroage account key from the target account.
        /// </summary>
        /// <param name="storageAccount">Target storage account.</param>
        /// <returns>Account key.</returns>
        Task<string> GetStorageAccountKey(
            IStorageAccount storageAccount);

        /// <summary>
        /// Given a specific storage type, derive the mountable file name.
        /// </summary>
        /// <param name="storageType">Target stroage type.</param>
        /// <returns>Mountable File Name.</returns>
        string GetStorageMountableFileName(
            StorageType storageType);

        /// <summary>
        /// Gets the stroage mountable share name.
        /// </summary>
        /// <returns>Mountable Share Name.</returns>
        string GetStorageMountableShareName();

        /// <summary>
        /// Fetch blob references by resource info and name.
        /// </summary>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <param name="storageAccountKey">Target storage account key if available.</param>
        /// <param name="blobContainerName">Target blob container name.</param>
        /// <param name="blobName">Target blob name if known.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<(CloudBlockBlob Blob, CloudBlobContainer BlobContainer)> FetchBlobAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            string blobName,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Fetch blob sas token.
        /// </summary>
        /// <param name="blob">Target blob reference.</param>
        /// <param name="blobContainer">Target blob container reference.</param>
        /// <param name="blobPermissions">Target blob permissions.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        (string Token, string BlobName, string BlobContainerName) FetchBlobSasToken(
            CloudBlockBlob blob,
            CloudBlobContainer blobContainer,
            SharedAccessBlobPermissions blobPermissions,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Build fully qualified sas token for file share.
        /// </summary>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <param name="storageAccountKey">Target storage account key if known.</param>
        /// <param name="storageType">Target storage type.</param>
        /// <param name="filePermissions">Target blob permissions.</param>
        /// <param name="filePrefix">Target file prefix.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        Task<(string Token, string FileShareName, string FileName)> FetchStorageFileShareSasTokenAsync(
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            StorageType storageType,
            SharedAccessFilePermissions filePermissions,
            string filePrefix,
            IDiagnosticsLogger logger);
    }
}
