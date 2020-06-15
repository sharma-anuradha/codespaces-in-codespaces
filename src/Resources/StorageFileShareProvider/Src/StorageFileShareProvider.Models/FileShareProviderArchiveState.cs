// <copyright file="FileShareProviderArchiveState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// States for the suspend operation.
    /// </summary>
    public enum FileShareProviderArchiveState
    {
        /// <summary>
        /// Prepare a file share for copying.
        /// </summary>
        ArchiveFileShare,

        /// <summary>
        /// Check that the fileshare has been copied.
        /// </summary>
        CheckBlob,
    }
}
