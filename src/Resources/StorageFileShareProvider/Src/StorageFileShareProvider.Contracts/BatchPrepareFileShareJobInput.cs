// <copyright file="BatchPrepareFileShareJobInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Batch Prepare File Share Job Input.
    /// </summary>
    public class BatchPrepareFileShareJobInput : BatchTaskInput
    {
        /// <summary>
        /// Gets or sets the array of storage items to copy.
        /// </summary>
        public IEnumerable<StorageCopyItem> SourceCopyItems { get; set; }

        /// <summary>
        /// Gets or sets the azure storage size in GB.
        /// </summary>
        public int StorageSizeInGb { get; set; }
    }
}
