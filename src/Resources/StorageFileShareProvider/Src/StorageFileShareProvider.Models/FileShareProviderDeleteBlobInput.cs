// <copyright file="FileShareProviderDeleteBlobInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider delete operation.
    /// </summary>
    public class FileShareProviderDeleteBlobInput : FileShareProviderDeleteInput
    {
        /// <summary>
        /// Gets or sets the storage account key.
        /// </summary>
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Gets or sets the blob container name.
        /// </summary>
        public string BlobContainerName { get; set; }

        /// <summary>
        /// Gets or sets the blob name.
        /// </summary>
        public string BlobName { get; set; }
    }
}
