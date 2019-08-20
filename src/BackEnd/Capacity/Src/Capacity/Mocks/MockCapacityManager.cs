// <copyright file="MockCapacityManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks
{
    /// <summary>
    /// The mock capacity manager.
    /// </summary>
    public class MockCapacityManager : ICapacityManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockCapacityManager"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        public MockCapacityManager(IAzureSubscriptionCatalog azureSubscriptionCatalog)
        {
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
        }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetComputeUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            return new AzureResourceUsage[]
            {
                new AzureResourceUsage(subscription.SubscriptionId, location, "mock-compute-quota", 100, 10),
            };
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetNetworkUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            return new AzureResourceUsage[]
            {
                new AzureResourceUsage(subscription.SubscriptionId, location, "mock-network-quota", 100, 10),
            };
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetStorageUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            return new AzureResourceUsage[]
            {
                new AzureResourceUsage(subscription.SubscriptionId, location, "mock-storage-quota", 100, 10),
            };
        }

        /// <inheritdoc/>
        public async Task<IAzureResourceLocation> SelectAzureResourceLocation(ICloudEnvironmentSku sku, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(sku, nameof(sku));
            Requires.NotNull(logger, nameof(logger));

            var subscription = AzureSubscriptionCatalog.AzureSubscriptions.FirstOrDefault(s => s.Locations.Contains(location));
            if (subscription is null)
            {
                throw new SkuNotAvailableException(sku.SkuName, location);
            }

            var resourceGroupName = $"mock-{location.ToString().ToLowerInvariant()}-resources";

            return new AzureResourceLocation(subscription, resourceGroupName, location);
        }
    }
}
