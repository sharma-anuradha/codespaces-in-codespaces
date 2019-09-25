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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// The capacity manager.
    /// </summary>
    public class CapacityManager : ICapacityManager
    {
        private Random Rnd { get; } = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityManager"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="controlPlaneInfo">The control-plane resource accessor.</param>
        /// <param name="resourceNameBuilder">resource name builder.</param>
        /// <param name="capacitySettings">Capacity settings.</param>
        public CapacityManager(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IControlPlaneInfo controlPlaneInfo,
            IResourceNameBuilder resourceNameBuilder,
            CapacitySettings capacitySettings)
        {
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            CapacitySettings = Requires.NotNull(capacitySettings, nameof(capacitySettings));
        }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private CapacitySettings CapacitySettings { get; }

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
        public async Task<IAzureResourceLocation> SelectAzureResourceLocation(ICloudEnvironmentSku _, AzureLocation location, IDiagnosticsLogger logger)
        {
            await Task.CompletedTask;
            /*
             * // The pool manager doesn't track the cloud environment sku, just the azure sku, so it cannot pass this value in. Accept null for now.
             * Requires.NotNull(sku, nameof(sku));
             * */
            Requires.NotNull(logger, nameof(logger));

            var subscription = AzureSubscriptionCatalog
                .AzureSubscriptions.Where(s => s.Locations.Contains(location)).RandomOrDefault();
            if (subscription is null)
            {
                // TODO: add more information to this exception if possible -- why not available.
                throw new SkuNotAvailableException(_?.SkuName ?? "unknown", location);
            }

            var stampResourceGroupName = ControlPlaneInfo.Stamp.StampResourceGroupName;
            var resourceGroupName = ResourceNameBuilder.GetResourceGroupName(stampResourceGroupName);

            // We'll used resource group names that match the stamp resource group, so that it will be clear which
            // stamp has allocated the data-plane resource groups. For example, production in East US would yield the name
            // vsclk-online-prod-rel-use-###
            if (CapacitySettings.SpreadResourcesInGroups)
            {
                var resourceGroupNumber = GetRandomResourceGroupNubmer();
                resourceGroupName = $"{resourceGroupName}-{resourceGroupNumber:000}";
            }

            return new AzureResourceLocation(subscription, resourceGroupName, location);
        }

        private int GetRandomResourceGroupNubmer()
        {
            return Rnd.Next(CapacitySettings.Min, CapacitySettings.Max);
        }
    }
}
