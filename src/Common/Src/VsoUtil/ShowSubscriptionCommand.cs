// <copyright file="ShowSubscriptionCommand.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.VsoUtil
{
    /// <summary>
    /// The show subscriptions verb.
    /// </summary>
    [Verb("subscriptions", HelpText = "Show the subscription catalog.")]
    public class ShowSubscriptionCommand : CommandBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether to include the subscription capacity.
        /// </summary>
        [Option('c', "show-capacity", HelpText = "Show the subscription capacity.")]
        public bool ShowCapacity { get; set; }

        /// <inheritdoc/>
        protected override void ExecuteCommand(IServiceProvider services, TextWriter stdout, TextWriter stderr)
        {
            var azureSubscriptionCatalog = services.GetRequiredService<IAzureSubscriptionCatalog>();
            WriteAzureSubscriptionCatalog(azureSubscriptionCatalog, services, ShowCapacity, stdout).Wait();
        }

        private async Task WriteAzureSubscriptionCatalog(
            IAzureSubscriptionCatalog azureSubscriptionCatalog,
            IServiceProvider serviceProvider,
            bool showCapacity,
            TextWriter textWriter)
        {
            var subscriptionDictionary = azureSubscriptionCatalog.AzureSubscriptions
                .OrderBy(s => s.DisplayName)
                .ToDictionary(item => item.DisplayName, item => item);

            if (showCapacity)
            {
                var controlPlaneInfo = (IControlPlaneInfo)serviceProvider.GetService(typeof(IControlPlaneInfo));
                var keyVaultName = controlPlaneInfo.EnvironmentKeyVaultName;
                var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net";
                await UpdateAppServicePrincipalClientSecret(serviceProvider, keyVaultUrl);
                var location = controlPlaneInfo.Stamp.Location;
                var subscriptionCapacityProvider = (IAzureSubscriptionCapacityProvider)serviceProvider.GetService(typeof(IAzureSubscriptionCapacityProvider));

                var taskDictionary = new Dictionary<string, Task<AzureResourceUsage[]>>();
                foreach (var subscription in subscriptionDictionary.Values)
                {
                    var key = subscription.DisplayName;
                    var usagesTask = GetUsagesAsync(location, subscriptionCapacityProvider, subscription);
                    taskDictionary.Add(
                        subscription.DisplayName,
                        usagesTask);
                }

                await Task.WhenAll(taskDictionary.Values);
                var capacityDictionary = new SortedDictionary<string, AzureResourceUsage[]>(
                    taskDictionary.ToDictionary(item => item.Key, item => item.Value.Result));

                // Compute aggregates
                const string aggregate = "aggregate";
                var allUsagesByQuota = capacityDictionary.Values.SelectMany(items => items).GroupBy(item => item.Quota);
                var aggregateUsages = new List<AzureResourceUsage>();
                foreach (var allUsages in allUsagesByQuota)
                {
                    var quota = allUsages.Key;
                    var limit = allUsages.Sum(item => item.Limit);
                    var current = allUsages.Sum(item => item.CurrentValue);
                    var usage = new AzureResourceUsage(aggregate, allUsages.First().ServiceType, location, quota, limit, current);
                    aggregateUsages.Add(usage);
                }

                capacityDictionary.Add(aggregate, aggregateUsages
                            .OrderBy(u => u.ServiceType)
                            .ThenBy(u => u.Quota)
                            .ToArray());

                var capacity = JsonSerializeObject(capacityDictionary);
                textWriter.WriteLine(capacity);
            }
            else
            {
                var subscriptions = JsonSerializeObject(subscriptionDictionary);
                textWriter.WriteLine(subscriptions);
            }
        }

        private async Task<AzureResourceUsage[]> GetUsagesAsync(
            AzureLocation location,
            IAzureSubscriptionCapacityProvider subscriptionCapacityProvider,
            IAzureSubscription subscription)
        {
            var computUsageTask = subscriptionCapacityProvider.GetAzureResourceUsageAsync(subscription, location, ServiceType.Compute, new NullLogger());
            var networkUsageTask = subscriptionCapacityProvider.GetAzureResourceUsageAsync(subscription, location, ServiceType.Network, new NullLogger());
            var storageUsageTask = subscriptionCapacityProvider.GetAzureResourceUsageAsync(subscription, location, ServiceType.Storage, new NullLogger());
            await Task.WhenAll(computUsageTask, networkUsageTask, storageUsageTask);

            var usages = new List<AzureResourceUsage>();

            var computUsage = computUsageTask.Result;
            usages.AddRange(computUsage);

            var networkUsage = networkUsageTask.Result;
            usages.AddRange(networkUsage);

            var storageUsage = storageUsageTask.Result;
            usages.AddRange(storageUsage);

            var orderedUsages = usages
                    .OrderBy(u => u.ServiceType)
                    .ThenBy(u => u.Quota)
                    .ToArray();

            return orderedUsages;
        }

        private async Task UpdateAppServicePrincipalClientSecret(IServiceProvider serviceProvider, string keyVaultUrl)
        {
            try
            {
                var secretProvider = (ISecretProvider)serviceProvider.GetService(typeof(ISecretProvider));
                var appSecretsProvider = (CommonAppSecretsProvider)secretProvider;
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                using (var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
                {
                    var appSpPassword = await keyVaultClient.GetSecretAsync($"{keyVaultUrl}/secrets/app-sp-password");
                    appSecretsProvider.AppServicePrincipalClientSecret = appSpPassword.Value;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not read the app service principal client secret from key vault ${keyVaultUrl}. You may need to JIT in and set the key vault Access Policies to 'Secrets {{Get, List}}': {ex.Message}", ex);
            }
        }
    }
}
