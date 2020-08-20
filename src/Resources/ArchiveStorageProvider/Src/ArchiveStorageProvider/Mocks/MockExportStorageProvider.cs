// <copyright file="MockExportStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Mocks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Mocks
{
    /// <summary>
    /// Mock export storage provider (for internal/test use only).
    /// </summary>
    internal class MockExportStorageProvider : IExportStorageProvider
    {
        private readonly IAzureSubscriptionCatalog subscriptionCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockExportStorageProvider"/> class.
        /// </summary>
        /// <param name="subscriptionCatalog">The subscription catalog.</param>
        public MockExportStorageProvider(
                IAzureSubscriptionCatalog subscriptionCatalog)
        {
            this.subscriptionCatalog = subscriptionCatalog;
        }

        /// <inheritdoc/>
        public async Task<ISharedStorageInfo> GetExportStorageAccountAsync(AzureLocation location, int minimumRequiredGB, IDiagnosticsLogger logger, bool forceCapacityCheck)
        {
            await Task.CompletedTask;
            return new MockExportStorageInfo(subscriptionCatalog, location);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ISharedStorageInfo>> ListExportStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            return new[] { new MockExportStorageInfo(subscriptionCatalog, location) };
        }

        /// <inheritdoc/>
        Task<ISharedStorageInfo> ISharedStorageProvider.GetStorageAccountAsync(AzureLocation location, int minimumRequiredGB, IDiagnosticsLogger logger, bool forceCapacityCheck)
        {
            return GetExportStorageAccountAsync(location, minimumRequiredGB, logger, forceCapacityCheck);
        }

        /// <inheritdoc/>
        Task<IEnumerable<ISharedStorageInfo>> ISharedStorageProvider.ListStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            return ListExportStorageAccountsAsync(location, logger);
        }
    }
}
