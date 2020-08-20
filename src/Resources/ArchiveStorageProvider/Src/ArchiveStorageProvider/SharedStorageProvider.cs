// <copyright file="SharedStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SharedStorageProvider
{
    /// <inheritdoc/>
    public abstract class SharedStorageProvider : ISharedStorageProvider
    {
        private const string StorageAccountNameKey = "storageAccountName";
        private const string ResourceGroupKey = "resourceGroup";
        private const string SubscriptionIdKey = "subscriptionId";
        private const string UsedCapacityInGbKey = "usedCapacityInGb";

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedStorageProvider"/> class.
        /// </summary>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="azureClientFactory">The azure client factory.</param>
        /// <param name="metricsProvider">The azure metrics provider.</param>
        /// <param name="resourceNameBuilder">Resource naming for DEV stamps.</param>
        /// <param name="personalStampSettings">DEV stamp settings.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        /// <param name="accountSkuName">Account sku name.</param>
        public SharedStorageProvider(
            ICapacityManager capacityManager,
            IControlPlaneInfo controlPlaneInfo,
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IAzureClientFactory azureClientFactory,
            IMetricsProvider metricsProvider,
            IResourceNameBuilder resourceNameBuilder,
            DeveloperPersonalStampSettings personalStampSettings,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(capacityManager, nameof(capacityManager));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            AzureSubscriptionCatalog = Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            AzureClientFactory = Requires.NotNull(azureClientFactory, nameof(azureClientFactory));
            MetricsProvider = Requires.NotNull(metricsProvider, nameof(metricsProvider));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            IsDeveloperStamp = Requires.NotNull(personalStampSettings, nameof(personalStampSettings)).DeveloperStamp;
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            Requires.NotNull(defaultLogValues, nameof(defaultLogValues));

            // Warmup the archive or export storage by ensuring that all storage accounts exist.
            var logger = diagnosticsLoggerFactory.New(defaultLogValues);
            InitializeStorageInfosTask = InitializeStorageInfosAsync(capacityManager, logger);
        }

        /// <summary>
        /// Gets instance of intializing storage info task.
        /// </summary>
        protected Task<IReadOnlyList<SharedStorageInfo>> InitializeStorageInfosTask { get; }

        /// <summary>
        /// Gets storage account SKU to create.
        /// </summary>
        protected abstract SkuName StorageAccountSkuName { get; }

        /// <summary>
        /// Gets storage account per region per subscription.
        /// </summary>
        protected abstract int StorageAccountsPerRegionPerSubscription { get; }

        /// <summary>
        /// GetsStorage account capacity.
        /// </summary>
        protected abstract double StorageAccountMaxCapacityInGb { get; }

        /// <summary>
        /// Gets Storage capacity message.
        /// </summary>
        protected abstract string StorageCapacityMessageLog { get; }

        /// <summary>
        /// Gets Storage account initialization message.
        /// </summary>
        protected abstract string StorageInitStorageAccountMessageLog { get; }

        /// <summary>
        /// Gets control plane info.
        /// </summary>
        protected IControlPlaneInfo ControlPlaneInfo { get; }

        /// <summary>
        /// Gets Resource name builder.
        /// </summary>
        protected IResourceNameBuilder ResourceNameBuilder { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IInMemoryManagedCache InMemoryManagedCache { get; }

        private IAzureClientFactory AzureClientFactory { get; }

        private IMetricsProvider MetricsProvider { get; }

        private bool IsDeveloperStamp { get; }

        /// <inheritdoc/>
        public async Task<ISharedStorageInfo> GetStorageAccountAsync(
            AzureLocation location,
            int minimumRequiredGB,
            IDiagnosticsLogger logger,
            bool forceCapacityCheck = false)
        {
            // This parameter is current ignored because the implementation doesn't cache capacity values.
            // We may add caching if the load on the Azure Metrics API ends up being too much.
            // If we add caching, the caller may request non-cached capacity values.
            _ = forceCapacityCheck;

            // The the list in random order
            var storageInfosInRandomOrder = (await InitializeStorageInfosTask)
                .Where(si => si.AzureLocation == location)
                .Shuffle()
                .ToArray();

            // Find the first an (random) account with capacity
            foreach (var storageInfo in storageInfosInRandomOrder)
            {
                if (await StorageAccountHasCapacity(storageInfo.AzureResourceInfo, minimumRequiredGB, logger.NewChildLogger()))
                {
                    return storageInfo;
                }
            }

            // Otherwise, we're out of storage capacity!
            throw new CapacityNotAvailableException(location, new[] { AzureResourceQuotaNames.StorageAccounts });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ISharedStorageInfo>> ListStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            var storageInfos = (await InitializeStorageInfosTask)
                .Where(storageInfo => storageInfo.AzureLocation == location);
            return storageInfos;
        }

        /// <summary>
        /// Get storage account name.
        /// </summary>
        /// <param name="location"> Azure location. </param>
        /// <param name="index"> Index. </param>
        /// <returns>Storage account name. </returns>
        public abstract string GetStorageAccountName(AzureLocation location, int index);

        /// <summary>
        /// Wait for the warmup task to be completed.
        /// </summary>
        /// <returns>Task.</returns>
        internal Task WarmupCompletedAsync()
        {
            return InitializeStorageInfosTask;
        }

        /// <summary>
        /// Perform background initialization for either the archive or export storage provider.
        /// Ensures that storage accounts exist for this control-plane's data planes.
        /// </summary>
        /// <param name="capacityManager"> Capacity manager. </param>
        /// <param name="logger"> Logger. </param>
        /// <returns>Task.</returns>
        protected async Task<IReadOnlyList<SharedStorageInfo>> InitializeStorageInfosAsync(ICapacityManager capacityManager, IDiagnosticsLogger logger)
        {
            Requires.NotNull(capacityManager, nameof(capacityManager));
            Requires.NotNull(logger, nameof(logger));

            // Ensure that the resource group exists for this stamp-data-plane
            var infrastructureSubscription = AzureSubscriptionCatalog.InfrastructureSubscription;
            var azure = await GetAzureClientAsync(infrastructureSubscription);
            var resourceGroupName = GetInfrastructureResourceGroupName();
            await EnsureResourceGroupExistsWithRetryAsync(azure, resourceGroupName, ControlPlaneInfo.Stamp.Location);

            // Generate the names / resourceInfos for the required storage accounts
            var infosAndLocations = ControlPlaneInfo.Stamp.DataPlaneLocations
                .SelectMany(location => Enumerable.Range(0, GetStorageAccountsPerRegionPerSubscription())
                    .Select(index => GetStorageAccountName(location, index))
                    .Select(storageAccountName =>
                        new
                        {
                            AzureResourceInfo = new AzureResourceInfo(infrastructureSubscription.SubscriptionId, resourceGroupName, storageAccountName),
                            Location = location,
                        }));

            // Generate tasks to ensure each account exists
            var storageInfos = await Task.WhenAll(infosAndLocations.Select(async info =>
            {
                return await logger.OperationScopeAsync(
                    StorageInitStorageAccountMessageLog,
                    async (innerLogger) =>
                    {
                        innerLogger
                            .FluentAddValue(SubscriptionIdKey, info.AzureResourceInfo.SubscriptionId)
                            .FluentAddValue(ResourceGroupKey, info.AzureResourceInfo.ResourceGroup)
                            .FluentAddValue(StorageAccountNameKey, info.AzureResourceInfo.Name);

                        // IAzure impl isn't thread safe. Create an inner one for each task.
                        var azureInner = await GetAzureClientAsync(infrastructureSubscription);
                        var (storageAccount, key) = await EnsureStorageAccountExistsWithRetryAsync(
                            azureInner,
                            info.AzureResourceInfo.ResourceGroup,
                            info.AzureResourceInfo.Name,
                            info.Location);

                        return new SharedStorageInfo(info.AzureResourceInfo, info.Location, key);
                    },
                    swallowException: true);
            }));

            return storageInfos
                .Where(info => info != default)
                .ToList()
                .AsReadOnly();
        }

        private Task<IAzure> GetAzureClientAsync(IAzureSubscription subscription)
        {
            return AzureClientFactory.GetAzureClientAsync(Guid.Parse(subscription.SubscriptionId));
        }

        private Task EnsureResourceGroupExistsWithRetryAsync(IAzure azure, string resourceGroup, AzureLocation location)
        {
            return Retry.DoAsync(async count =>
            {
                await azure.CreateResourceGroupIfNotExistsAsync(resourceGroup, location.ToString());
            });
        }

        private async Task<(IStorageAccount, string)> EnsureStorageAccountExistsWithRetryAsync(
            IAzure azure,
            string resourceGroup,
            string storageAccountName,
            AzureLocation azureLocation)
        {
            var location = azureLocation.ToString();
            var skuName = StorageAccountSkuName.ToString();

            var storageAccount = await Retry.DoAsync(async count =>
            {
                var sa = await azure.CreateStorageAccountIfNotExistsAsync(resourceGroup, location, storageAccountName, skuName);

                // Wait until done creating...
                while (sa.ProvisioningState == ProvisioningState.Creating)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                return sa;
            });

            var keys = await GetStorageAccountKeysWithRetryAsync(storageAccount);

            return (storageAccount, keys.First().Value);
        }

        private Task<IReadOnlyList<StorageAccountKey>> GetStorageAccountKeysWithRetryAsync(IStorageAccount storageAccount)
        {
            return Retry.DoAsync(async count =>
            {
                return await storageAccount.GetKeysAsync();
            });
        }

        private int GetStorageAccountsPerRegionPerSubscription() => IsDeveloperStamp ? 1 : StorageAccountsPerRegionPerSubscription;

        private string GetInfrastructureResourceGroupName()
        {
            var stampResourceGroupName = ControlPlaneInfo.Stamp.StampInfrastructureResourceGroupName;
            var resourceGroupName = ResourceNameBuilder.GetResourceGroupName(stampResourceGroupName);
            return resourceGroupName;
        }

        private async Task<bool> StorageAccountHasCapacity(AzureResourceInfo info, int minimumRequiredGB, IDiagnosticsLogger logger)
        {
            var duration = logger.StartDuration();
            var result = await MetricsProvider.GetAzureStorageAverageUsedCapacityInGbAsync(
                info.SubscriptionId.ToString(),
                info.ResourceGroup,
                info.Name,
                logger);

            var usedCapacityInGb = result.GetValueOrDefault();
            var remainingCapacityInGb = (int)(StorageAccountMaxCapacityInGb - usedCapacityInGb);

            logger.AddDuration(duration)
                .FluentAddValue(SubscriptionIdKey, info.SubscriptionId)
                .FluentAddValue(ResourceGroupKey, info.ResourceGroup)
                .FluentAddValue(StorageAccountNameKey, info.Name)
                .FluentAddValue(UsedCapacityInGbKey, usedCapacityInGb)
                .LogInfo(StorageCapacityMessageLog);

            return remainingCapacityInGb > minimumRequiredGB;
        }
    }
}
