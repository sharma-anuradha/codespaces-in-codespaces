﻿// <copyright file="FileShareProviderAssignResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
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
        /// <param name="storageFileServiceHost"><see cref="StorageFileServiceHost"/>.</param>
        public FileShareProviderAssignResult(string storageAccountName, string storageAccountKey, string storageShareName, string storageFileName, string storageFileServiceHost)
        {
            StorageAccountName = storageAccountName;
            StorageAccountKey = storageAccountKey;
            StorageShareName = storageShareName;
            StorageFileName = storageFileName;
            StorageFileServiceHost = storageFileServiceHost;
        }

        /// <summary>
        /// Gets or sets the Azure storage account name.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the Azure storage account key.
        /// </summary>
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Gets or sets the share name of a share in the storage account.
        /// </summary>
        public string StorageShareName { get; set; }

        /// <summary>
        /// Gets or sets the file name of a file in the share.
        /// </summary>
        public string StorageFileName { get; set; }

        /// <summary>
        /// Gets or sets the file service host name of the storage account.
        /// </summary>
        public string StorageFileServiceHost { get; set; }
    }
}
