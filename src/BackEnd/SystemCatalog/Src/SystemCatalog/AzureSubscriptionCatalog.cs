// <copyright file="AzureSubscriptionCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
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
        public AzureSubscriptionCatalog(IOptions<AzureSubscriptionCatalogOptions> azureSubscriptionCatalogOptions)
            : this(azureSubscriptionCatalogOptions.Value.Settings)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscriptionCatalog"/> class.
        /// </summary>
        /// <param name="azureSubscriptionCatalogSettings">The Azure subscription catalog settings, most likely obtained from AppSettings.</param>
        public AzureSubscriptionCatalog(AzureSubscriptionCatalogSettings azureSubscriptionCatalogSettings)
        {
            Requires.NotNull(azureSubscriptionCatalogSettings, nameof(azureSubscriptionCatalogSettings));

            // Create the ordered, immutable list, same for all configured subscriptions.
            var locations = new ReadOnlyCollection<AzureLocation>(azureSubscriptionCatalogSettings.DefaultLocations
                .Distinct()
                .OrderBy(l => Enum.GetName(typeof(AzureLocation), l))
                .ToList());

            foreach (var azureSubscriptionSettings in azureSubscriptionCatalogSettings.AzureSubscriptions)
            {
                var id = Guid.Parse(azureSubscriptionSettings.SubscriptionId);
                if (Subscriptions.ContainsKey(id))
                {
                    throw new InvalidOperationException($"A subscription with the id '{id}' already exists.");
                }

                var servicePrincipalSettings = azureSubscriptionSettings.ServicePrincipal;
                var servicePrincipal = new ServicePrincipal(
                    servicePrincipalSettings.ClientId,
                    servicePrincipalSettings.ClientSecretKeyVaultSecretIdentifier,
                    servicePrincipalSettings.TenantId,
                    ResolveKeyvaultSecret);

                var azureSubscription = new AzureSubscription(
                    azureSubscriptionSettings.SubscriptionId,
                    azureSubscriptionSettings.DisplayName,
                    servicePrincipal,
                    azureSubscriptionSettings.Enabled,
                    locations); // future--would be possible to override locations per subscription, but not needed now

                Subscriptions.Add(id, azureSubscription);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IAzureSubscription> AzureSubscriptions => Subscriptions.Values
            .OrderBy(item => item.SubscriptionId)
            .ToArray();

        private Dictionary<Guid, IAzureSubscription> Subscriptions { get; } = new Dictionary<Guid, IAzureSubscription>();

        private async Task<string> ResolveKeyvaultSecret(string clientSecretKeyVaultSecretIdentifier)
        {
            await Task.CompletedTask;

            // TODO: Temporary hack: the secret identifier is just an environment variable name where we look up the secert.
            // TODO: Eventually we'll actually call keyvault to get the secrets.
            var secret = Environment.GetEnvironmentVariable(clientSecretKeyVaultSecretIdentifier);
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException($"The environment variable '{clientSecretKeyVaultSecretIdentifier}' is not set.");
            }

            return secret;
        }
    }
}
