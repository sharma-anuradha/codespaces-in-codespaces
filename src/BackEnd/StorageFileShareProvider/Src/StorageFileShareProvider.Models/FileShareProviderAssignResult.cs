// <copyright file="FileShareProviderAssignResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Result of the provider assign operation.
    /// </summary>
    public class FileShareProviderAssignResult : ContinuationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareProviderAssignResult"/> class.
        /// </summary>
        public FileShareProviderAssignResult()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileShareProviderAssignResult"/> class.
        /// </summary>
        /// <param name="storageAccountName"><see cref="StorageAccountName"/>.</param>
        /// <param name="storageAccountKey"><see cref="StorageAccountKey"/>.</param>
        /// <param name="storageShareName"><see cref="StorageShareName"/>.</param>
        /// <param name="storageFileName"><see cref="StorageFileName"/>.</param>
        public FileShareProviderAssignResult(string storageAccountName, string storageAccountKey, string storageShareName, string storageFileName)
        {
            StorageAccountName = storageAccountName;
            StorageAccountKey = storageAccountKey;
            StorageShareName = storageShareName;
            StorageFileName = storageFileName;
        }

        /// <summary>
        /// Gets the Azure storage account name.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets the Azure storage account key.
        /// </summary>
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Gets the share name of a share in the storage account.
        /// </summary>
        public string StorageShareName { get; set; }

        /// <summary>
        /// Gets the file name of a file in the share.
        /// </summary>
        public string StorageFileName { get; set; }
    }
}
