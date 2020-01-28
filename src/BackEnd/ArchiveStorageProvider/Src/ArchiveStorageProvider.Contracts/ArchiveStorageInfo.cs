// <copyright file="ArchiveStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <inheritdoc/>
    public class ArchiveStorageInfo : IArchiveStorageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveStorageInfo"/> class.
        /// </summary>
        /// <param name="azureResourceInfo">The resource info of the storage account.</param>
        /// <param name="storageAccountKey">The storage account key.</param>
        public ArchiveStorageInfo(AzureResourceInfo azureResourceInfo, string storageAccountKey)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNullOrEmpty(storageAccountKey, nameof(storageAccountKey));

            AzureResourceInfo = azureResourceInfo;
            StorageAccountKey = storageAccountKey;
        }

        /// <inheritdoc/>
        public AzureResourceInfo AzureResourceInfo { get; }

        /// <inheritdoc/>
        public string StorageAccountKey { get; }
    }
}
