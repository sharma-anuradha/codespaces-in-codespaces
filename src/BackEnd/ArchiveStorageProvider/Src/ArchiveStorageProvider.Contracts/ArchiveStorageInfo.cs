// <copyright file="ArchiveStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <inheritdoc/>
    public class ArchiveStorageInfo : IArchiveStorageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveStorageInfo"/> class.
        /// </summary>
        /// <param name="azureResourceLocation">The resource location of the storage account.</param>
        /// <param name="storageAccountName">The storage account name.</param>
        public ArchiveStorageInfo(IAzureResourceLocation azureResourceLocation, string storageAccountName)
        {
            Requires.NotNull(azureResourceLocation, nameof(azureResourceLocation));
            Requires.NotNullOrEmpty(storageAccountName, nameof(storageAccountName));

            AzureResourceLocation = azureResourceLocation;
            StorageAccountName = storageAccountName;
        }

        /// <inheritdoc/>
        public IAzureResourceLocation AzureResourceLocation { get; }

        /// <inheritdoc/>
        public string StorageAccountName { get; }
    }
}
