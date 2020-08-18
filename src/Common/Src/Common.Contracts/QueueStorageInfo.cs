// <copyright file="QueueStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents queue storage information.
    /// </summary>
    public class QueueStorageInfo
    {
        /// <summary>
        /// Gets or sets the subscription id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group name.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the storage account name.
        /// </summary>
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Gets or sets the storage account key.
        /// </summary>
        public string StorageAccountKey { get; set; }
    }
}
