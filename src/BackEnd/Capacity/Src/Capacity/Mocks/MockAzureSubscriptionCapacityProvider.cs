// <copyright file="MockAzureSubscriptionCapacityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Mocks
{
    /// <summary>
    /// The mock capacity manager.
    /// </summary>
    public class MockAzureSubscriptionCapacityProvider : IAzureSubscriptionCapacityProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockAzureSubscriptionCapacityProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        public MockAzureSubscriptionCapacityProvider()
        {
        }

        /// <inheritdoc/>
        public Task<IEnumerable<AzureResourceUsage>> GetAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc/>
        public Task UpdateAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            throw new System.NotImplementedException();
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetComputeUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            var result = new List<AzureResourceUsage>();

            foreach (var computeQuota in subscription.ComputeQuotas)
            {
                var quota = computeQuota.Key;
                var desiredLimit = computeQuota.Value;
                if (desiredLimit > 0)
                {
                    result.Add(new AzureResourceUsage(subscription.SubscriptionId, ServiceType.Compute, location, quota, desiredLimit, desiredLimit / 2));
                }
            }

            return result;
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetNetworkUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            var result = new List<AzureResourceUsage>();

            foreach (var networkQuota in subscription.NetworkQuotas)
            {
                var quota = networkQuota.Key;
                var desiredLimit = networkQuota.Value;
                if (desiredLimit > 0)
                {
                    result.Add(new AzureResourceUsage(subscription.SubscriptionId, ServiceType.Network, location, quota, desiredLimit, desiredLimit / 2));
                }
            }

            return result;
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetStorageUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            var result = new List<AzureResourceUsage>();

            foreach (var storageQuota in subscription.StorageQuotas)
            {
                var quota = storageQuota.Key;
                var desiredLimit = storageQuota.Value;
                if (desiredLimit > 0)
                {
                    result.Add(new AzureResourceUsage(subscription.SubscriptionId, ServiceType.Storage, location, quota, desiredLimit, desiredLimit / 2));
                }
            }

            return result;
        }
    }
}
