// <copyright file="FileShareProviderCreateInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class FileShareProviderCreateInput
    {
        /// <summary>
        /// 
        /// </summary>
        public string AzureSubscription { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AzureLocation { get; set; } // 'westus2'

        /// <summary>
        /// 
        /// </summary>
        public string AzureResourceGroup { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string StorageSeedBlobUrl { get; set; }
    }
}