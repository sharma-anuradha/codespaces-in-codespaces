// <copyright file="MockExportStorageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Mocks
{
    /// <summary>
    /// Mock archive storage info (for internal/test use only).
    /// </summary>
    internal class MockExportStorageInfo : ISharedStorageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockExportStorageInfo"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The subscription catalog.</param>
        /// <param name="location">The azure location for this storage account.</param>
        public MockExportStorageInfo(IAzureSubscriptionCatalog azureSubscriptionCatalog, AzureLocation location)
        {
            // Pick a subscription...
            var subscription = azureSubscriptionCatalog.AzureSubscriptions.Where(s => s.Locations.Contains(location)).First();
            AzureResourceInfo = new AzureResourceInfo(subscription.SubscriptionId, "MockResourceGroup", "MockStorageAccountName");
            StorageAccountKey = "MockExportStorageAccountKey";
        }

        /// <inheritdoc/>
        public AzureResourceInfo AzureResourceInfo { get; }

        /// <inheritdoc/>
        public AzureLocation AzureLocation { get; }

        /// <inheritdoc/>
        public string StorageAccountKey { get; }
    }
}
