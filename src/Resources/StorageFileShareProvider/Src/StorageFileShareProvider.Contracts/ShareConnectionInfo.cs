// <copyright file="ShareConnectionInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Connection information to connect (or mount) the file share.
    /// </summary>
    public class ShareConnectionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShareConnectionInfo"/> class.
        /// </summary>
        public ShareConnectionInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShareConnectionInfo"/> class.
        /// </summary>
        /// <param name="storageAccountName"><see cref="StorageAccountName"/>.</param>
        /// <param name="storageAccountKey"><see cref="StorageAccountKey"/>.</param>
        /// <param name="storageShareName"><see cref="StorageShareName"/>.</param>
        /// <param name="storageFileName"><see cref="StorageFileName"/>.</param>
        /// <param name="storageFileServiceHost"><see cref="StorageFileServiceHost"/>.</param>
        public ShareConnectionInfo(
            string storageAccountName,
            string storageAccountKey,
            string storageShareName,
            string storageFileName,
            string storageFileServiceHost)
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