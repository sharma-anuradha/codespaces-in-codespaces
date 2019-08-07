// <copyright file="FileShareProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider create operation.
    /// </summary>
    public class FileShareProviderCreateInput
    {
        /// <summary>
        /// Gets or sets Azure Subscription Id.
        /// </summary>
        public string AzureSubscription { get; set; }

        /// <summary>
        /// Gets or sets Azure location (e.g. "westus2").
        /// </summary>
        public string AzureLocation { get; set; }

        /// <summary>
        /// Gets or sets Azure resource group.
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the blob url used for file share preparation.
        /// </summary>
        public string StorageBlobUrl { get; set; }
    }
}