// <copyright file="FileShareProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// Input for the provider create operation.
    /// </summary>
    public class FileShareProviderCreateInput : ContinuationInput
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
        /// Gets or sets the azure sku name that should be targeted.
        /// </summary>
        public string AzureSkuName { get; set; }

        /// <summary>
        /// Gets or sets resource tags that should be added to the resource.
        /// </summary>
        public IDictionary<string, string> ResourceTags { get; set; }

        /// <summary>
        /// Gets or sets the array of items to copy to the file share.
        /// </summary>
        public IEnumerable<StorageCopyItem> StorageCopyItems { get; set; }

        /// <summary>
        /// Gets or sets storage size.
        /// </summary>
        public int StorageSizeInGb { get; set; }
    }
}