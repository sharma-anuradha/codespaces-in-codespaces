﻿// <copyright file="AzureSubscriptionCatalog.cs" company="Microsoft">
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

            var applicatonServicePrincipal = azureSubscriptionCatalogOptions.ApplicationServicePrincipal;
            var dataPlaneSettings = azureSubscriptionCatalogOptions.DataPlaneSettings;
            var defaultQuotas = dataPlaneSettings.DefaultQuotas;

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

                // Create the ordered list of locations.
                var locations = new ReadOnlyCollection<AzureLocation>(azureSubscriptionSettings.Locations
                    .Distinct()
                    .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                    .ToList()
                    .AsReadOnly());

                // Get the service principal for this subscription, or use the application default.
                var servicePrincipalSettings = azureSubscriptionSettings.ServicePrincipal ?? applicatonServicePrincipal;
                if (servicePrincipalSettings is null)
                {
                    throw new InvalidOperationException($"A service principal has not been configured for subscription '{id}'");
                }

                var servicePrincipal = new ServicePrincipal(
                    servicePrincipalSettings.ClientId,
                    servicePrincipalSettings.ClientSecretName,
                    servicePrincipalSettings.TenantId,
                    SecretProvider);

                var computeQuotas = defaultQuotas.Compute.Combine(azureSubscriptionSettings.Quotas?.Compute);
                var storageQuotas = defaultQuotas.Storage.Combine(azureSubscriptionSettings.Quotas?.Storage);
                var networkQuotas = defaultQuotas.Network.Combine(azureSubscriptionSettings.Quotas?.Network);

                var azureSubscription = new AzureSubscription(
                    azureSubscriptionSettings.SubscriptionId,
                    subscriptionName,
                    servicePrincipal,
                    azureSubscriptionSettings.Enabled,
                    locations,
                    computeQuotas,
                    storageQuotas,
                    networkQuotas);

                Subscriptions.Add(id, azureSubscription);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IAzureSubscription> AzureSubscriptions => Subscriptions.Values
            .OrderBy(item => item.SubscriptionId)
            .ToArray();

        private Dictionary<Guid, IAzureSubscription> Subscriptions { get; } = new Dictionary<Guid, IAzureSubscription>();

        private ISecretProvider SecretProvider { get; }
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
