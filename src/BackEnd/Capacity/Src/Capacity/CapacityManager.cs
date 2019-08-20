// <copyright file="CapacityManager.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// The capacity manager.
    /// </summary>
    public class CapacityManager : ICapacityManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityManager"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        public CapacityManager(IAzureSubscriptionCatalog azureSubscriptionCatalog)
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
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetNetworkUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetStorageUsageAsync(IAzureSubscription subscription, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));
            throw new NotImplementedException();
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
                // TODO: add more information to this exception if possible -- why not available.
                throw new SkuNotAvailableException(sku.SkuName, location);
            }

            // TODO: how to shard across multiple resource groups?
            // It doesn't make sense to create a new one for every request, or for every cloud-environment because
            // there is a limit of ~900 per subscription.
            // It doesn't make sense to use one resoruce groups per locations because there is a limit of
            // 800 resource per group.
            // Could include sku.SkuName + location?
            var resourceGroupName = $"{location.ToString().ToLowerInvariant()}-resources";

            return new AzureResourceLocation(subscription, resourceGroupName, location);
        }
    }
}
