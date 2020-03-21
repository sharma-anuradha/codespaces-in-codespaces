﻿// <copyright file="IStorageFileShareProviderHelperExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions
{
    /// <summary>
    /// Storage File Share Provider Helper Extensions.
    /// </summary>
    public static class IStorageFileShareProviderHelperExtensions
    {
        /// <summary>
        /// Fetch archive blob references by resource info and name.
        /// </summary>
        /// <param name="helper">Target helper.</param>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <param name="name">Target base name.</param>
        /// <param name="storageAccountKey">Target storage account key if available.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static Task<(CloudBlockBlob Blob, CloudBlobContainer BlobContainer)> FetchArchiveBlobAsync(
            this IStorageFileShareProviderHelper helper,
            AzureResourceInfo azureResourceInfo,
            string name,
            string storageAccountKey,
            IDiagnosticsLogger logger)
        {
            // Keeping the blob and container name in lock step
            var blobContainerName = BuildArchiveBlobName(name);
            var blobName = blobContainerName;

            return helper.FetchBlobAsync(azureResourceInfo, storageAccountKey, blobContainerName, blobName, logger);
        }

        /// <summary>
        /// Fetch archive blob references by resource info and name.
        /// </summary>
        /// <param name="helper">Target helper.</param>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <param name="name">Target base name.</param>
        /// <param name="storageAccountKey">Target storage account key if available.</param>
        /// <param name="blobPermissions">Target blob permissions.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<(string Token, string BlobName, string BlobContainerName)> FetchArchiveBlobSasTokenAsync(
            this IStorageFileShareProviderHelper helper,
            AzureResourceInfo azureResourceInfo,
            string name,
            string storageAccountKey,
            SharedAccessBlobPermissions blobPermissions,
            IDiagnosticsLogger logger)
        {
            // Get reference
            var reference = await helper.FetchArchiveBlobAsync(azureResourceInfo, name, storageAccountKey, logger);

            return helper.FetchBlobSasToken(reference.Blob, reference.BlobContainer, blobPermissions, logger);
        }

        /// <summary>
        /// Fetch blob sas token.
        /// </summary>
        /// <param name="helper">Target helper.</param>
        /// <param name="azureResourceInfo">Target azure resource info.</param>
        /// <param name="storageAccountKey">Target storage account key if available.</param>
        /// <param name="blobContainerName">Target blob container name.</param>
        /// <param name="blobName">Target blob name if known.</param>
        /// <param name="blobPermissions">Target blob permissions.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<(string Token, string BlobName, string BlobContainerName)> FetchBlobSasTokenAsync(
            this IStorageFileShareProviderHelper helper,
            AzureResourceInfo azureResourceInfo,
            string storageAccountKey,
            string blobContainerName,
            string blobName,
            SharedAccessBlobPermissions blobPermissions,
            IDiagnosticsLogger logger)
        {
            // Get reference
            var reference = await helper.FetchBlobAsync(azureResourceInfo, storageAccountKey, blobContainerName, blobName, logger);

            return helper.FetchBlobSasToken(reference.Blob, reference.BlobContainer, blobPermissions, logger);
        }

        private static string BuildArchiveBlobName(string accountName)
        {
            accountName = accountName.Replace("_", string.Empty).Replace("-", string.Empty);

            return $"archive-{accountName}".ToLowerInvariant();
        }
    }
}
