// <copyright file="StorageType.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Represents a platform specific storage type.
    /// </summary>
    public enum StorageType
    {
        /// <summary>
        /// The Linux storage type e.g. an ext4 blob.
        /// </summary>
        Linux,

        /// <summary>
        /// The Windows storage type e.g. a VHD file.
        /// </summary>
        Windows,
    }
}
