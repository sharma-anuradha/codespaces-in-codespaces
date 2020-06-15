// <copyright file="ArchiveEnvironmentContinuationInputState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Archive Operation State.
    /// </summary>
    public enum ArchiveEnvironmentContinuationInputState
    {
        /// <summary>
        /// When the storage blob needs to be allocated.
        /// </summary>
        AllocateStorageBlob = 0,

        /// <summary>
        /// When the storage blob should be should start to be copied.
        /// </summary>
        StartStorageBlob = 10,

        /// <summary>
        /// When the storage blob copy has started and we need to be checking progress status.
        /// </summary>
        CheckStartStorageBlob = 20,

        /// <summary>
        /// When the storage blob copy has completed and we need to cleanup.
        /// </summary>
        CleanupUnneededStorage = 30,
    }
}
