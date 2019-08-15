// <copyright file="StoragePrepareException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Exception for errors during preparation of storage.
    /// </summary>
    public class StoragePrepareException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StoragePrepareException"/> class.
        /// </summary>
        /// <param name="azureStorageAccountId">Azure Resource Id of the storage account that was being operated on.</param>
        /// <param name="storagePath">Path to the storage file that was being operated on.</param>
        /// <param name="status">Status message of the prepare operation.</param>
        /// <param name="statusDescription">Human-readable status message of the prepare operation.</param>
        public StoragePrepareException(string azureStorageAccountId, string storagePath, string status, string statusDescription)
            : base("Storage file share preparation has terminated in error state")
        {
            AzureStorageAccountId = azureStorageAccountId;
            StoragePath = storagePath;
            Status = status;
            StatusDescription = statusDescription;
        }

        /// <summary>
        /// Gets the Azure Resource Id of the storage account.
        /// </summary>
        public string AzureStorageAccountId { get; }

        /// <summary>
        /// Gets the path to the storage file.
        /// </summary>
        public string StoragePath { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public string Status { get; }

        /// <summary>
        /// Gets the status description.
        /// </summary>
        public string StatusDescription { get; }
    }
}
