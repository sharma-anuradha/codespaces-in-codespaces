// <copyright file="FileShareProviderCreateState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// States for the create operation.
    /// </summary>
    public enum FileShareProviderCreateState
    {
        /// <summary>
        /// Create a storage account.
        /// </summary>
        CreateStorageAccount,

        /// <summary>
        /// Create a file share in a storage account.
        /// </summary>
        CreateFileShare,

        /// <summary>
        /// Prepare a file share in a storage account.
        /// </summary>
        PrepareFileShare,

        /// <summary>
        /// Check a file share in a storage account for prepare completion.
        /// </summary>
        CheckFileShare,
    }
}
