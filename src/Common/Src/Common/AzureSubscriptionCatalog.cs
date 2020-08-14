// <copyright file="AzureSubscriptionCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Implements <see cref="IAzureSubscriptionCatalog"/> based on <see cref="AzureSubscriptionCatalogSettings"/> from AppSettings.
    /// </summary>
    public class AzureSubscriptionCatalog : IAzureSubscriptionCatalog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscriptionCatalog"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalogOptions">The options instance for the azure subscriptions catalog.</param>
        /// <param name="secretProvider">The secret provider.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public AzureSubscriptionCatalog(
            IOptions<AzureSubscriptionCatalogOptions> azureSubscriptionCatalogOptions,
            ISecretProvider secretProvider)
            : this(azureSubscriptionCatalogOptions.Value, secretProvider)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscriptionCatalog"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalogOptions">The azure subscription catalog options.</param>
        /// <param name="secretProvider">The secret provider.</param>
        /// <param name="logger">The diagnostics logger.</param>
        public AzureSubscriptionCatalog(
            AzureSubscriptionCatalogOptions azureSubscriptionCatalogOptions,
            ISecretProvider secretProvider)
        {
            Requires.NotNull(azureSubscriptionCatalogOptions, nameof(azureSubscriptionCatalogOptions));
            Requires.NotNull(azureSubscriptionCatalogOptions.DataPlaneSettings, nameof(azureSubscriptionCatalogOptions.DataPlaneSettings));

            SecretProvider = Requires.NotNull(secretProvider, nameof(secretProvider));

            var applicationServicePrincipal = azureSubscriptionCatalogOptions.ApplicationServicePrincipal;
            var dataPlaneSettings = azureSubscriptionCatalogOptions.DataPlaneSettings;
            var defaultQuotas = dataPlaneSettings.DefaultQuotas;
            var defaultLocations = dataPlaneSettings.DefaultLocations;

            // Set up the infrastructure subscription
            var infrastructureSubscription = dataPlaneSettings.InfrastructureSubscription;
            InfrastructureSubscription = CreateAzureSubscriptionObject(
                    infrastructureSubscription,
                    infrastructureSubscription.SubscriptionName ?? "(infrastructure)",
                    applicationServicePrincipal,
                    defaultLocations,
                    defaultQuotas);

            // Set up the general purpose data-plane subscriptions
            foreach (var item in dataPlaneSettings.Subscriptions)
            {
                var subscriptionName = item.Key;
                var azureSubscriptionSettings = item.Value;

                // Get the subscription ID and make sure that it isn't a duplicate.
                var id = Guid.Parse(azureSubscriptionSettings.SubscriptionId);
                if (Subscriptions.ContainsKey(id))
                {
                    throw new InvalidOperationException($"A subscription with the id '{id}' already exists.");
                }

                var azureSubscription = CreateAzureSubscriptionObject(
                    azureSubscriptionSettings,
                    subscriptionName,
                    applicationServicePrincipal,
                    defaultLocations,
                    defaultQuotas);

                Subscriptions.Add(id, azureSubscription);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IAzureSubscription> AzureSubscriptions => Subscriptions.Values
            .OrderBy(item => item.SubscriptionId)
            .ToArray();

        /// <inheritdoc/>
        public IAzureSubscription InfrastructureSubscription { get; }

        private Dictionary<Guid, IAzureSubscription> Subscriptions { get; } = new Dictionary<Guid, IAzureSubscription>();

        private ISecretProvider SecretProvider { get; }

        private AzureSubscription CreateAzureSubscriptionObject(
            AzureSubscriptionSettings azureSubscriptionSettings,
            string subscriptionName,
            ServicePrincipalSettings applicatonServicePrincipal,
            List<AzureLocation> defaultLocations,
            AzureSubscriptionQuotaSettings defaultQuotas)
        {
            // Create the ordered list of locations.
            var whichLocations = azureSubscriptionSettings.Locations?.Any() == true ?
                azureSubscriptionSettings.Locations : defaultLocations;
            var locations = new ReadOnlyCollection<AzureLocation>(whichLocations
                .Distinct()
                .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                .ToList()
                .AsReadOnly());

            // Get the service principal for this subscription, or use the application default.
            var servicePrincipalSettings = azureSubscriptionSettings.ServicePrincipal ?? applicatonServicePrincipal;
            if (servicePrincipalSettings is null)
            {
                throw new InvalidOperationException($"A service principal has not been configured for subscription '{azureSubscriptionSettings.SubscriptionId}'");
            }

            var servicePrincipal = new ServicePrincipal(
                servicePrincipalSettings.ClientId,
                servicePrincipalSettings.ClientSecretName,
                servicePrincipalSettings.TenantId,
                SecretProvider,
                servicePrincipalSettings.ObjectId);

            Dictionary<string, int> computeQuotas = default;
            Dictionary<string, int> storageQuotas = default;
            Dictionary<string, int> networkQuotas = default;

            switch (azureSubscriptionSettings.ServiceType)
            {
                case ServiceType.Compute:
                    computeQuotas = defaultQuotas.Compute.Combine(azureSubscriptionSettings.Quotas?.Compute);
                    break;
                case ServiceType.Storage:
                    storageQuotas = defaultQuotas.Storage.Combine(azureSubscriptionSettings.Quotas?.Storage);
                    break;
                case ServiceType.Network:
                    networkQuotas = defaultQuotas.Network.Combine(azureSubscriptionSettings.Quotas?.Network);
                    break;
                case ServiceType.KeyVault:
                    break;
                default:
                    computeQuotas = defaultQuotas.Compute.Combine(azureSubscriptionSettings.Quotas?.Compute);
                    storageQuotas = defaultQuotas.Storage.Combine(azureSubscriptionSettings.Quotas?.Storage);
                    networkQuotas = defaultQuotas.Network.Combine(azureSubscriptionSettings.Quotas?.Network);
                    break;
            }

            var azureSubscription = new AzureSubscription(
                azureSubscriptionSettings.SubscriptionId,
                subscriptionName,
                servicePrincipal,
                azureSubscriptionSettings.Enabled,
                locations,
                computeQuotas,
                storageQuotas,
                networkQuotas,
                azureSubscriptionSettings.ServiceType);
            return azureSubscription;
        }
    }

#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// Dictionary extension methods.
    /// </summary>
    internal static class DictionaryCombine
    {
        /// <summary>
        /// Copy the first dictionary and overlay the items of the second.
        /// </summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <param name="first">The first dictionary, whose items are fully copied.</param>
        /// <param name="second">The second dictionary, whose items are copied over the first.</param>
        /// <returns>The new dictionary.</returns>
        public static Dictionary<TKey, TValue> Combine<TKey, TValue>(
            this IDictionary<TKey, TValue> first,
            IDictionary<TKey, TValue> second)
        {
            var result = new Dictionary<TKey, TValue>(first);

            if (second != null)
            {
                foreach (var item in second)
                {
                    result[item.Key] = item.Value;
                }
            }

            return result;
        }
    }
}
