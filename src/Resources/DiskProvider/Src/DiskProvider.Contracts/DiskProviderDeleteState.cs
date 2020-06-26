// <copyright file="DiskProviderDeleteState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts
{
    /// <summary>
    /// States for the delete operation of the disk.
    /// </summary>
    public enum DiskProviderDeleteState
    {
        /// <summary>
        /// Checks if the disk is still attached to the VM.
        /// </summary>
        CheckAttachedDisk,

        /// <summary>
        /// Delete the disk.
        /// </summary>
        BeginDeleteDisk,

        /// <summary>
        /// Check for the disk deletion.
        /// </summary>
        CheckDeletedDiskState,
    }
}
