// <copyright file="ShareConnectionInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider
{
    /// <summary>
    /// Connection information to connect (or mount) the file share.
    /// </summary>
    public class ShareConnectionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShareConnectionInfo"/> class.
        /// </summary>
        /// <param name="storageAccountName"><see cref="StorageAccountName"/>.</param>
        /// <param name="storageAccountKey"><see cref="StorageAccountKey"/>.</param>
        /// <param name="storageShareName"><see cref="StorageShareName"/>.</param>
        /// <param name="storageFileName"><see cref="StorageFileName"/>.</param>
        public ShareConnectionInfo(string storageAccountName, string storageAccountKey, string storageShareName, string storageFileName)
        {
            StorageAccountName = storageAccountName;
            StorageAccountKey = storageAccountKey;
            StorageShareName = storageShareName;
            StorageFileName = storageFileName;
        }

        /// <summary>
        /// Gets the Azure storage account name.
        /// </summary>
        public string StorageAccountName { get; }

        /// <summary>
        /// Gets the Azure storage account key.
        /// </summary>
        public string StorageAccountKey { get; }

        /// <summary>
        /// Gets the share name of a share in the storage account.
        /// </summary>
        public string StorageShareName { get; }

        /// <summary>
        /// Gets the file name of a file in the share.
        /// </summary>
        public string StorageFileName { get; }
    }
}