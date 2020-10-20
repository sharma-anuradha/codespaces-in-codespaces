// <copyright file="AzureSubscriptionCapacityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
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

        private const int ArtificialKeyVaultLimit = 100;

        // Random number generator used for randomizing subscription allocation for resources that
        // has no azure quota limits, such as KeyVaults.
        private static readonly Random Random = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscriptionCapacityProvider"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The azure subscription catalog.</param>
        /// <param name="capacityRepository">The capacity repository.</param>
        /// <param name="servicePrincipal">The application service principal.</param>
        public AzureSubscriptionCapacityProvider(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            ICapacityRepository capacityRepository,
            IServicePrincipal servicePrincipal)
        {
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            Requires.NotNull(capacityRepository, nameof(capacityRepository));
            Requires.NotNull(servicePrincipal, nameof(servicePrincipal));

            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            CapacityRepository = capacityRepository;
            ServicePrincipal = servicePrincipal;
        }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private ICapacityRepository CapacityRepository { get; }

        private IServicePrincipal ServicePrincipal { get; }

        private static string MakeLoggingName(string operation) => $"{LoggingPrefix}_{operation}".ToLowerInvariant();

        /// <inheritdoc/>
        public Task<AzureResourceUsage> LoadAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, string quota, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNullOrEmpty(quota, nameof(quota));
            var id = CapacityRecord.MakeId(subscription.SubscriptionId, serviceType, location, quota);

            logger
                .AddSubscriptionId(subscription.SubscriptionId)
                .AddAzureLocation(location)
                .AddServiceType(serviceType)
                .AddQuota(quota);

            var operationName = MakeLoggingName(nameof(LoadAzureResourceUsageAsync));

            return logger.RetryOperationScopeAsync(
                operationName,
                async (childLogger) =>
                {
                    if (serviceType == ServiceType.KeyVault)
                    {
                        /*
                        // Theoretically there are no limits to the number of KeyVaults you can have in a subscription.
                        // This code constructs an artificial resource usage object for keyVaults, which can still
                        // preserve the randomness of how the subscription is choosen.
                        */

                        // TODO - this should be tracked properly

                        int artificialKeyVaultCurrentUsage = Random.Next(ArtificialKeyVaultLimit); // Returning random usage implies there will always be room for one more.

                        return new AzureResourceUsage(
                            subscription.SubscriptionId,
                            ServiceType.KeyVault,
                            location,
                            ServiceType.KeyVault.ToString(),
                            ArtificialKeyVaultLimit,
                            artificialKeyVaultCurrentUsage);
                    }

                    var capacity = await CapacityRepository.GetAsync(id, childLogger.NewChildLogger());

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
                (exception, childLogger) =>
                {
                    throw new CapacityNotFoundException(subscription, location, serviceType, quota, exception);
                });
        }

        /// <inheritdoc/>
        public async Task UpdateAzureResourceUsageAsync(IAzureSubscription subscription, AzureLocation location, ServiceType serviceType, IDiagnosticsLogger logger)
        {
            Requires.NotNull(subscription, nameof(subscription));
            Requires.NotNull(logger, nameof(logger));

            var operationName = MakeLoggingName($"get_{serviceType}_usage");

            // TODO: Target for RunBackgroundEnumerableAsync
            await logger.OperationScopeAsync(
                operationName,
                async (childLogger) =>
                {
                    var usages = await GetAzureResourceUsageAsync(subscription, location, serviceType, childLogger.NewChildLogger());

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

        /// <inheritdoc/>
        public async Task<IEnumerable<AzureResourceUsage>> GetAzureResourceUsageAsync(
            IAzureSubscription subscription,
            AzureLocation location,
            ServiceType serviceType,
            IDiagnosticsLogger logger)
        {
            logger = logger
                .NewChildLogger()
                .AddSubscriptionId(subscription.SubscriptionId)
                .AddAzureLocation(location)
                .AddServiceType(serviceType);

            IEnumerable<string> quotas = Enumerable.Empty<string>();

            try
            {
                switch (serviceType)
                {
                    case ServiceType.Compute:
                        quotas = subscription.ComputeQuotas.Keys;
                        return await GetAzureComputeUsageAsync(subscription, location, quotas);

                    case ServiceType.Storage:
                        quotas = subscription.StorageQuotas.Keys;
                        return await GetAzureStorageUsageAsync(subscription, location, quotas);

                    case ServiceType.Network:
                        quotas = subscription.NetworkQuotas.Keys;
                        return await GetAzureNetworkUsageAsync(subscription, location, quotas);

                    case ServiceType.KeyVault:
                        quotas = new[] { AzureResourceQuotaNames.KeyVaults };
                        return await GetAzureKeyVaultUsageAsync(subscription, location);

                    default:
                        throw new NotSupportedException();
                }
            }
            catch (CloudException ex) when (ex.Body.Code == "SubscriptionNotFound")
            {
                logger
                    .AddErrorDetail(ex.ToString())
                    .LogWarning(MakeLoggingName("get_azure_resource_usage_error"));

                // Return a default of zero for each quota
                return quotas
                    .Select(quota => new AzureResourceUsage(
                        subscription.SubscriptionId,
                        serviceType,
                        location,
                        quota,
                        limit: 0,
                        currentValue: 0))
                    .ToArray();
            }
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
        }

        private async Task<IEnumerable<AzureResourceUsage>> GetAzureKeyVaultUsageAsync(
           IAzureSubscription subscription,
           AzureLocation location)
        {
            // If service type of this subscription isn't "KeyVault" return empty.
            if (subscription.ServiceType != null && subscription.ServiceType != ServiceType.KeyVault)
            {
                return new List<AzureResourceUsage>();
            }

            const int maxKeyVaultsPerResourceGroup = 800;
            var keyVaultLimit = maxKeyVaultsPerResourceGroup * subscription.MaxResourceGroupCount;

            using (var keyVaultClient = await CreateKeyVaultManagementClientAsync(subscription))
            {
                var keyVaults = await keyVaultClient.Vaults.ListAsync();

                var totalKeyVaults = keyVaults
                    .Where((kv) => string.Equals(kv.Location, location.ToString(), StringComparison.OrdinalIgnoreCase))
                    .Count();

                var azureResourceUsage = 
                    new AzureResourceUsage(
                        subscription.SubscriptionId,
                        ServiceType.KeyVault,
                        location,
                        AzureResourceQuotaNames.KeyVaults,
                        keyVaultLimit,
                        totalKeyVaults);

                return new[] { azureResourceUsage };
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

        private async Task<KeyVaultManagementClient> CreateKeyVaultManagementClientAsync(IAzureSubscription azureSubscription)
        {
            var restClient = await CreateRestClientAsync();
            var keyVaultClient = new KeyVaultManagementClient(restClient)
            {
                SubscriptionId = azureSubscription.SubscriptionId,
            };
            return keyVaultClient;
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
