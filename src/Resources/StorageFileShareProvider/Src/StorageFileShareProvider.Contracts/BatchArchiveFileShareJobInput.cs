// <copyright file="BatchArchiveFileShareJobInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Batch Suspend File Share Job Input.
    /// </summary>
    public class BatchArchiveFileShareJobInput : BatchTaskInput
    {
        /// <summary>
        /// Gets or sets the source url.
        /// </summary>
        public string SrcFileShareUriWithSas { get; set; }

        /// <summary>
        /// Gets or sets the destination url.
        /// </summary>
        public string DestBlobUriWithSas { get; set; }
    }
}
