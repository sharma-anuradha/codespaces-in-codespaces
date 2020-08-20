// <copyright file="MockArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Mocks
{
    /// <summary>
    /// Mock archive storage provider (for internal/test use only).
    /// </summary>
    internal class MockArchiveStorageProvider : IArchiveStorageProvider
    {
        private readonly IAzureSubscriptionCatalog subscriptionCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockArchiveStorageProvider"/> class.
        /// </summary>
        /// <param name="subscriptionCatalog">The subscription catalog.</param>
        public MockArchiveStorageProvider(
                IAzureSubscriptionCatalog subscriptionCatalog)
        {
            this.subscriptionCatalog = subscriptionCatalog;
        }

        /// <inheritdoc/>
        public async Task<ISharedStorageInfo> GetArchiveStorageAccountAsync(AzureLocation location, int minimumRequiredGB, IDiagnosticsLogger logger, bool forceCapacityCheck)
        {
            await Task.CompletedTask;
            return new MockArchiveStorageInfo(subscriptionCatalog, location);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ISharedStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            return new[] { new MockArchiveStorageInfo(subscriptionCatalog, location) };
        }
    }
}
