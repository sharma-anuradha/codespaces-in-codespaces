// <copyright file="AzureSubscriptionCapacityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ServiceBus.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity
{
    /// <summary>
    /// The azure subscription capacity provider.
    /// Queries capacity from ARM and stores into cosmos db.
    /// </summary>
    public class AzureSubscriptionCapacityProvider : IAzureSubscriptionCapacityProvider
    {
        /// <summary>
        /// Specifies how long to keep cached capacity data cached in memory.
        /// </summary>
        public static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Specifies how frequently to update the azure capacity data.
        /// </summary>
        public static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Specifies how long to maintain the update lease before another worker can try.
        /// </summary>
        public static readonly TimeSpan UpdateLeaseInterval = TimeSpan.FromMinutes(4);

        private const string LoggingPrefix = "azure_subscription_capacity_provider";
        private const string VirtualNetworksQuota = AzureResourceQuotaNames.VirtualNetworks;
        private const int VirtualNetworksDefaultLimit = 1000;
        private const string StorageAccountsQuota = AzureResourceQuotaNames.StorageAccounts;
        private const int StorageAccountsDefaultLimit = 250;
        private const int ArtificialKeyVaultLimit = 100; // Key vaults have no limit, but we'll return something reasonable for the caller to consider.

        // Random number generator used for randomizing subscription allocation for resources that
        // has no azure quota limits, such as KeyVaults.
        private static readonly Random Random = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscriptionCapacityProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="capacityRepository">The capacity repository.</param>
        /// <param name="controlPlaneInfo">The control-plane resource accessor.</param>
        /// <param name="servicePrincipal">The application service principal.</param>
        /// <param name="taskHelper">The task helper.</param>
        public AzureSubscriptionCapacityProvider(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ICapacityRepository capacityRepository,
            IControlPlaneInfo controlPlaneInfo,
            IServicePrincipal servicePrincipal,
            ITaskHelper taskHelper)
        {
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            Requires.NotNull(capacityRepository, nameof(capacityRepository));
            Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            Requires.NotNull(taskHelper, nameof(taskHelper));

            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            CapacityRepository = capacityRepository;
            ControlPlaneInfo = controlPlaneInfo;
            ServicePrincipal = servicePrincipal;
            TaskHelper = taskHelper;
        }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private ICapacityRepository CapacityRepository { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IServicePrincipal ServicePrincipal { get; }

        private ITaskHelper TaskHelper { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> LoadAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            logger = logger.WithValues(
                new LogValueSet
                {
                    { nameof(subscription), subscription.SubscriptionId },
                    { nameof(location), location.ToString() },
                    { nameof(serviceType), serviceType.ToString() },
                });

            switch (serviceType)
            {
                case ServiceType.Compute:
                    return await LoadAzureResourceUsageAsync(subscription, location, ServiceType.Compute, subscription.ComputeQuotas, logger);

                case ServiceType.Storage:
                    return await LoadAzureResourceUsageAsync(subscription, location, ServiceType.Storage, subscription.StorageQuotas, logger);

                case ServiceType.Network:
                    return await LoadAzureResourceUsageAsync(subscription, location, ServiceType.Network, subscription.NetworkQuotas, logger);

                case ServiceType.KeyVault:
                    /*
                    // Theoretically there are no limits to the number of KeyVaults you can have in a subscription.
                    // This code constructs an artificial resource usage object for keyVaults, which can still
                    // preserve the randomness of how the subscription is choosen.
                    */

                    int artificialKeyVaultCurrentUsage = Random.Next(ArtificialKeyVaultLimit); // Returning random usage implies there will always be room for one more.

                    var resourceUsage = new AzureResourceUsage(
                        subscription.SubscriptionId,
                        ServiceType.KeyVault,
                        location,
                        ServiceType.KeyVault.ToString(),
                        ArtificialKeyVaultLimit,
                        artificialKeyVaultCurrentUsage);

                    return Enumerable.Repeat(resourceUsage, 1);

                default:
                    throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            var operationName = MakeLoggingName($"get_{serviceType}_usage");

            switch (serviceType)
            {
                case ServiceType.Compute:
                    await UpdateAzureResourceUsageInternalAsync(
                        logger,
                        operationName,
                        () => GetAzureComputeUsageAsync(subscription, location, subscription.ComputeQuotas.Keys));
                    break;

                case ServiceType.Storage:
                    await UpdateAzureResourceUsageInternalAsync(
                        logger,
                        operationName,
                        () => GetAzureStorageUsageAsync(subscription, location, subscription.StorageQuotas.Keys));
                    break;

                case ServiceType.Network:
                    await UpdateAzureResourceUsageInternalAsync(
                        logger,
                        operationName,
                        () => GetAzureNetworkUsageAsync(subscription, location, subscription.NetworkQuotas.Keys));
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetAzureResourceUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            ServiceType serviceType,
            IDiagnosticsLogger logger)
        {
            logger = logger.WithValues(
                new LogValueSet
                {
                    { nameof(subscription), subscription.SubscriptionId },
                    { nameof(location), location.ToString() },
                    { nameof(serviceType), serviceType.ToString() },
                });

            switch (serviceType)
            {
                case ServiceType.Compute:
                    return await GetAzureComputeUsageAsync(subscription, location, subscription.ComputeQuotas.Keys);

                case ServiceType.Storage:
                    return await GetAzureStorageUsageAsync(subscription, location, subscription.StorageQuotas.Keys);

                case ServiceType.Network:
                    return await GetAzureNetworkUsageAsync(subscription, location, subscription.NetworkQuotas.Keys);

                default:
                    throw new NotSupportedException();
            }
        }

        private static string MakeLoggingName(string operation) => $"{LoggingPrefix}_{operation}".ToLowerInvariant();

        private async Task UpdateAzureResourceUsageInternalAsync(
            IDiagnosticsLogger logger,
            string operationName,
            Func<Task<IEnumerable<AzureResourceUsage>>> getUsageFuncAsync)
        {
            // TODO: Target for RunBackgroundEnumerableAsync
            await logger.OperationScopeAsync(
                operationName,
                async (childLogger) =>
                {
                    var usages = await getUsageFuncAsync();

                    foreach (var usage in usages)
                    {
                        await Retry.DoAsync(
                            async (attempt) =>
                            {
                                var capacityRecord = new CapacityRecord(usage);
                                await CapacityRepository.CreateOrUpdateAsync(capacityRecord, logger);
                            });
                    }
                });
        }

        private async Task<IEnumerable<AzureResourceUsage>> LoadAzureResourceUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            ServiceType serviceType,
            IReadOnlyDictionary<string, int> quotas,
            IDiagnosticsLogger logger)
        {
            var operationName = MakeLoggingName(nameof(LoadAzureResourceUsageAsync));

            return await logger.OperationScopeAsync(
                operationName,
                async (childLogger) =>
                {
                    var result = new List<AzureResourceUsage>();

                    foreach (var quotaItem in quotas)
                    {
                        var quota = quotaItem.Key;
                        var desiredLimit = quotaItem.Value;

                        if (desiredLimit > 0)
                        {
                            var usage = await LoadAzureResourceUsageAsync(subscription, location, serviceType, quota, childLogger.NewChildLogger());
                            result.Add(usage);
                        }
                    }

                    childLogger
                        .FluentAddValue(nameof(subscription), subscription.SubscriptionId)
                        .FluentAddValue(nameof(location), location.ToString())
                        .FluentAddValue(nameof(serviceType), serviceType.ToString());

                    return result;
                });
        }

        private async Task<AzureResourceUsage> LoadAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, string quota, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNullOrEmpty(quota, nameof(quota));
            var id = CapacityRecord.MakeId(subscription.SubscriptionId, serviceType, location, quota);

            return await Retry.DoAsync(
                async (attempt) =>
                {
                    var capacity = await CapacityRepository.GetAsync(id, logger);

                    // The record doesn't exist so we can't answer the question.
                    if (capacity is null)
                    {
                        throw new CapacityNotFoundException(subscription, location, serviceType, quota);
                    }

                    return new AzureResourceUsage(
                        capacity.SubscriptionId,
                        capacity.ServiceType,
                        capacity.Location,
                        capacity.Quota,
                        capacity.Limit,
                        capacity.CurrentValue);
                },
                (attempt, exception) =>
                {
                    throw new CapacityNotFoundException(subscription, location, serviceType, quota, exception);
                });
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetAzureComputeUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            IEnumerable<string> quotas)
        {
            // If service type of this subscription isn't "Compute" return empty.
            if (subscription.ServiceType != null && subscription.ServiceType != ServiceType.Compute)
            {
                return new List<AzureResourceUsage>();
            }

            using (var computeClient = await CreateComputeManagementAsync(subscription))
            {
                var usage = await computeClient.Usage.ListAsync(location.ToString());
                var usageArray = usage.ToArray();

                var azureResourceUsage = usageArray
                    .Where(computeUsage => quotas.Contains(computeUsage.Name.Value))
                    .Select(computeUsage =>
                        new AzureResourceUsage(
                            subscription.SubscriptionId,
                            ServiceType.Compute,
                            location,
                            computeUsage.Name.Value,
                            computeUsage.Limit,
                            computeUsage.CurrentValue));

                return azureResourceUsage;
            }
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetAzureNetworkUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            IEnumerable<string> quotas)
        {
            // If service type of this subscription isn't "Network" return empty.
            if (subscription.ServiceType != null && subscription.ServiceType != ServiceType.Network)
            {
                return new List<AzureResourceUsage>();
            }

            using (var networkClient = await CreateNetworkManagementClientAsync(subscription))
            {
                try
                {
                    var usage = await networkClient.Usages.ListAsync(location.ToString());
                    var usageArray = usage.ToArray();

                    var azureResourceUsage = usageArray
                        .Where(networkUsage => quotas.Contains(networkUsage.Name.Value))
                        .Select(networkUsage =>
                            new AzureResourceUsage(
                                subscription.SubscriptionId,
                                ServiceType.Network,
                                location,
                                networkUsage.Name.Value,
                                networkUsage.Limit,
                                networkUsage.CurrentValue));

                    return azureResourceUsage;
                }
                catch (CloudException ex) when (ex.Message.Contains("has no usages in NRP"))
                {
                    // The exception message is of the form "Subscription XXX has no usages in NRP."
                    // If no usage is reported, then return a default of zero.
                    var defaultUsage = new AzureResourceUsage(
                        subscription.SubscriptionId,
                        ServiceType.Network,
                        location,
                        VirtualNetworksQuota,
                        VirtualNetworksDefaultLimit,
                        0);
                    return new[] { defaultUsage };
                }
            }
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetAzureStorageUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            IEnumerable<string> quotas)
        {
            // If service type of this subscription isn't "Storage" return empty.
            if (subscription.ServiceType != null && subscription.ServiceType != ServiceType.Storage)
            {
                return new List<AzureResourceUsage>();
            }

            using (var storageClient = await CreateStorageManagementClientAsync(subscription))
            {
                try
                {
                    var usage = await storageClient.Usages.ListByLocationAsync(location.ToString());
                    var usageArray = usage.ToArray();

                    var azureResourceUsage = usageArray
                        .Where(storageUsage => quotas.Contains(storageUsage.Name.Value))
                        .Select(storageUsage =>
                            new AzureResourceUsage(
                                subscription.SubscriptionId,
                                ServiceType.Storage,
                                location,
                                storageUsage.Name.Value,
                                storageUsage.Limit.GetValueOrDefault(),
                                storageUsage.CurrentValue.GetValueOrDefault()));

                    return azureResourceUsage;
                }
                catch (CloudException ex) when (ex.Message.Contains("was not found"))
                {
                    // The exception message is of the form "Subscription XXX was not found."
                    // If no usage is reported, then return a default of zero.
                    var defaultUsage = new AzureResourceUsage(
                        subscription.SubscriptionId,
                        ServiceType.Storage,
                        location,
                        StorageAccountsQuota,
                        StorageAccountsDefaultLimit,
                        0);
                    return new[] { defaultUsage };
                }
            }
        }

#if false // unused, but might want this to count the number of resource groups, or resources per group
        private async Task<ResourceManagementClient> CreateResourceManagementClientAsync(IAzureSubscription azureSubscription)
        {
            var restClient = await CreateRestClientAsync();
            var resourceManagementClient = new ResourceManagementClient(restClient)
            {
                SubscriptionId = azureSubscription.SubscriptionId,
            };
            return resourceManagementClient;
        }
#endif

        private async Task<ComputeManagementClient> CreateComputeManagementAsync(IAzureSubscription azureSubscription)
        {
            var restClient = await CreateRestClientAsync();
            var computeClient = new ComputeManagementClient(restClient)
            {
                SubscriptionId = azureSubscription.SubscriptionId,
            };
            return computeClient;
        }

        private async Task<NetworkManagementClient> CreateNetworkManagementClientAsync(IAzureSubscription azureSubscription)
        {
            var restClient = await CreateRestClientAsync();
            var networkClient = new NetworkManagementClient(restClient)
            {
                SubscriptionId = azureSubscription.SubscriptionId,
            };
            return networkClient;
        }

        private async Task<StorageManagementClient> CreateStorageManagementClientAsync(IAzureSubscription azureSubscription)
        {
            var restClient = await CreateRestClientAsync();
            var storageClient = new StorageManagementClient(restClient)
            {
                SubscriptionId = azureSubscription.SubscriptionId,
            };
            return storageClient;
        }

        private async Task<RestClient> CreateRestClientAsync()
        {
            var servicePrincipalClientSecret = await ServicePrincipal.GetClientSecretAsync();
            var azureCredentials = new AzureCredentialsFactory()
                .FromServicePrincipal(
                    ServicePrincipal.ClientId,
                    servicePrincipalClientSecret,
                    ServicePrincipal.TenantId,
                    AzureEnvironment.AzureGlobalCloud);

            var restClient = RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithCredentials(azureCredentials)
                .Build();

            return restClient;
        }
    }
}
