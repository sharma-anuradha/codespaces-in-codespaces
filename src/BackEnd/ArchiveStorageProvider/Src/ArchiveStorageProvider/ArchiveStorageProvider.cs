// <copyright file="ArchiveStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Azure.Management.Storage.Fluent.Models;
using Microsoft.VsSaaS.Azure.Management;
using Microsoft.VsSaaS.Azure.Metrics;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ArchiveStorageProvider
{
    /// <inheritdoc/>
    public class ArchiveStorageProvider : IArchiveStorageProvider
    {
        // Storage Account SKU to create
        private const SkuName ArchiveStorageAccountSkuName = SkuName.StandardZRS;

        // Number of archive storage accounts to create per region.
        private const int ArchiveStorageAccountsPerRegionPerSubscription = 10;
        private const double StorageAccountMaxCapacityInGb = 1000; // 1TB
        private const string StorageAccountNameKey = "storageAccountName";
        private const string ResourceGroupKey = "resourceGroup";
        private const string SubscriptionIdKey = "subscriptionId";
        private const string UsedCapacityInGbKey = "usedCapacityInGb";
        private const string ArchiveStorageInitStorageAccountMessage = "archive_storage_init_storage_account";
        private const string ArchiveStorageCapacityMessage = "archive_storage_capacity";

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveStorageProvider"/> class.
        /// </summary>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="controlPlaneInfo">The control plane info.</param>
        /// <param name="systemCatalog">The system catalog.</param>
        /// <param name="metricsProvider">The azure metrics provider.</param>
        /// <param name="diagnosticsLoggerFactory">The diagnostics logger factory.</param>
        /// <param name="defaultLogValues">The default log values.</param>
        public ArchiveStorageProvider(
            ICapacityManager capacityManager,
            IControlPlaneInfo controlPlaneInfo,
            ISystemCatalog systemCatalog,
            IMetricsProvider metricsProvider,
            IDiagnosticsLoggerFactory diagnosticsLoggerFactory,
            LogValueSet defaultLogValues)
        {
            Requires.NotNull(capacityManager, nameof(capacityManager));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(systemCatalog, nameof(systemCatalog));
            Requires.NotNull(metricsProvider, nameof(metricsProvider));
            Requires.NotNull(diagnosticsLoggerFactory, nameof(diagnosticsLoggerFactory));
            Requires.NotNull(defaultLogValues, nameof(defaultLogValues));

            ControlPlaneInfo = controlPlaneInfo;
            AzureSubscriptionCatalog = systemCatalog.AzureSubscriptionCatalog;
            AzureClientFactory = new AzureClientFactory(systemCatalog);
            MetricsProvider = metricsProvider;

            // Warmup the archive storage by ensuring that all storage accounts exist.
            var logger = diagnosticsLoggerFactory.New(defaultLogValues);
            InitializeArchiveStorageInfosTask = InitializeArchiveStorageInfosAsync(capacityManager, logger);
        }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IInMemoryManagedCache InMemoryManagedCache { get; }

        private AzureClientFactory AzureClientFactory { get; }

        private IMetricsProvider MetricsProvider { get; }

        private Task<IReadOnlyList<ArchiveStorageInfo>> InitializeArchiveStorageInfosTask { get; }

        /// <inheritdoc/>
        public async Task<IArchiveStorageInfo> GetArchiveStorageAccountAsync(
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
            var storageInfosInRandomOrder = (await InitializeArchiveStorageInfosTask)
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

            // Otherwise, we're out of archive storage capacity!
            throw new CapacityNotAvailableException(location, new[] { AzureResourceQuotaNames.StorageAccounts });
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<IArchiveStorageInfo>> ListArchiveStorageAccountsAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            var storageInfos = (await InitializeArchiveStorageInfosTask)
                .Where(storageInfo => storageInfo.AzureLocation == location);
            return storageInfos;
        }

        /// <summary>
        /// Wait for the warmup task to be completed.
        /// </summary>
        /// <returns>Task.</returns>
        internal async Task WarmupCompletedAsync()
        {
            await InitializeArchiveStorageInfosTask;
        }

        /// <summary>
        /// Perform background initialization for the archive storage provider.
        /// Ensures that storage accounts exist for this control-plane's data planes.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task<IReadOnlyList<ArchiveStorageInfo>> InitializeArchiveStorageInfosAsync(ICapacityManager capacityManager, IDiagnosticsLogger logger)
        {
            Requires.NotNull(capacityManager, nameof(capacityManager));
            Requires.NotNull(logger, nameof(logger));

            // Ensure that the resource group exists for this stamp-data-plane
            var infrastructureSubscription = AzureSubscriptionCatalog.InfrastructureSubscription;
            var azure = await GetAzureClientAsync(infrastructureSubscription);
            var resourceGroupName = ControlPlaneInfo.Stamp.StampInfrastructureResourceGroupName;
            await EnsureResourceGroupExistsWithRetryAsync(azure, resourceGroupName, ControlPlaneInfo.Stamp.Location);

            // Generate the names / resourceInfos for the required storage accounts
            var infosAndLocations = ControlPlaneInfo.Stamp.DataPlaneLocations
                .SelectMany(location => Enumerable.Range(0, ArchiveStorageAccountsPerRegionPerSubscription)
                    .Select(index => ControlPlaneInfo.Stamp.GetDataPlaneStorageAccountNameForArchiveStorageName(location, index))
                    .Select(storageAccountName =>
                        new
                        {
                            AzureResourceInfo = new AzureResourceInfo(infrastructureSubscription.SubscriptionId, resourceGroupName, storageAccountName),
                            Location = location,
                        }));

            // Generate tasks to ensure each account exists
            var archiveStorageInfos = await Task.WhenAll(infosAndLocations.Select(async info =>
            {
                return await logger.OperationScopeAsync(
                    ArchiveStorageInitStorageAccountMessage,
                    async (innerLogger) =>
                    {
                        innerLogger
                            .FluentAddValue(SubscriptionIdKey, info.AzureResourceInfo.SubscriptionId)
                            .FluentAddValue(ResourceGroupKey, info.AzureResourceInfo.ResourceGroup)
                            .FluentAddValue(StorageAccountNameKey, info.AzureResourceInfo.Name);

                        // IAzure impl isn't thread safe. Create an inner one for each task.
                        var azureInner = await GetAzureClientAsync(infrastructureSubscription);
                        var (storageAccount, key) = await EnsureArchiveStorageAccountExistsWithRetryAsync(
                            azureInner,
                            info.AzureResourceInfo.ResourceGroup,
                            info.AzureResourceInfo.Name,
                            info.Location);

                        return new ArchiveStorageInfo(info.AzureResourceInfo, info.Location, key);
                    },
                    swallowException: true);
            }));

            return archiveStorageInfos
                .Where(info => info != default)
                .ToList()
                .AsReadOnly();
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
                .LogInfo(ArchiveStorageCapacityMessage);

            return remainingCapacityInGb > minimumRequiredGB;
        }

        private async Task<IAzure> GetAzureClientAsync(IAzureSubscription subscription)
        {
            return await AzureClientFactory.GetAzureClientAsync(Guid.Parse(subscription.SubscriptionId));
        }

        private async Task EnsureResourceGroupExistsWithRetryAsync(IAzure azure, string resourceGroup, AzureLocation location)
        {
            await Retry.DoAsync(async count =>
            {
                await azure.CreateResourceGroupIfNotExistsAsync(resourceGroup, location.ToString());
            });
        }

        private async Task<(IStorageAccount, string)> EnsureArchiveStorageAccountExistsWithRetryAsync(
            IAzure azure,
            string resourceGroup,
            string storageAccountName,
            AzureLocation azureLocation)
        {
            var location = azureLocation.ToString();
            var skuName = ArchiveStorageAccountSkuName.ToString();

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

        private async Task<IReadOnlyList<StorageAccountKey>> GetStorageAccountKeysWithRetryAsync(IStorageAccount storageAccount)
        {
            return await Retry.DoAsync(async count =>
            {
                return await storageAccount.GetKeysAsync();
            });
        }
    }
}
