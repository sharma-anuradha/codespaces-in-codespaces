// <copyright file="StorageCopyItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
using System;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts
{
    /// <summary>
    /// Represents an item to be copied.
    /// </summary>
    public class StorageCopyItem
    {
        /// <summary>
        /// Gets or sets the src blob URL to copy (including SAS token with read permission to blob).
        /// </summary>
        public string SrcBlobUrl { get; set; }

        /// <summary>
        /// Gets or sets the storage type.
        /// </summary>
        public StorageType StorageType { get; set; }

        /// <summary>
        /// Gets the src blob file name.
        /// </summary>
        public string SrcBlobFileName => new Uri(SrcBlobUrl).Segments.Last();
    }
}
