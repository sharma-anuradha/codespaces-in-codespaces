// <copyright file="AzureResourceInfoQueueDetailsProxy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts
{
    /// <summary>
    /// Azure resource info details for the queue.
    /// </summary>
    public class AzureResourceInfoQueueDetailsProxy
    {
        /// <summary>
        /// Key name for azure location.
        /// </summary>
        public const string LocationName = "location";

        /// <summary>
        /// Key name of the storage account.
        /// </summary>
        public const string StorageAccountName = "storageAccount";

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfoQueueDetailsProxy"/> class.
        /// </summary>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        public AzureResourceInfoQueueDetailsProxy(AzureResourceInfo azureResourceInfo)
        {
            AzureResourceInfo = Requires.NotNull(azureResourceInfo, nameof(azureResourceInfo));
        }

        /// <summary>
        /// Gets the azure location.
        /// </summary>
        public AzureLocation Location
        {
            get { return (AzureLocation)Enum.Parse(typeof(AzureLocation), AzureResourceInfo.Properties[LocationName], true); }
        }

        /// <summary>
        /// Gets the storage account name.
        /// </summary>
        public string StorageAccount
        {
            get { return AzureResourceInfo.Properties[StorageAccountName]; }
        }

        private AzureResourceInfo AzureResourceInfo { get; }
    }
}
