// <copyright file="PrepareFileShareStatus.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Prepare file share status.
    /// </summary>
    public enum PrepareFileShareStatus
    {
        /// <summary>
        /// Queued but not running yet
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Running
        /// </summary>
        Running = 1,

        /// <summary>
        /// Completed and was successful
        /// </summary>
        Succeeded = 2,

        /// <summary>
        /// Completed but failed
        /// </summary>
        Failed = 3,
    }
}