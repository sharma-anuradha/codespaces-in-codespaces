// <copyright file="ArchiveStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts
{
    /// <inheritdoc/>
    public class ArchiveStorageInfo : IArchiveStorageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveStorageInfo"/> class.
        /// </summary>
        /// <param name="azureResourceInfo">The resource info of the storage account.</param>
        /// <param name="azureLocation">The azure location of the storage account.</param>
        /// <param name="storageAccountKey">The storage account key.</param>
        public ArchiveStorageInfo(AzureResourceInfo azureResourceInfo, AzureLocation azureLocation, string storageAccountKey)
        {
            Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
            Requires.NotNull(storageAccountKey, nameof(storageAccountKey));

            AzureResourceInfo = azureResourceInfo;
            AzureLocation = azureLocation;
            StorageAccountKey = storageAccountKey;
        }

        /// <inheritdoc/>
        public AzureResourceInfo AzureResourceInfo { get; }

        /// <inheritdoc/>
        public AzureLocation AzureLocation { get; }

        /// <inheritdoc/>
        public string StorageAccountKey { get; }
    }
}
