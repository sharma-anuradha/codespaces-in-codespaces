// <copyright file="AzureSubscriptionCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Network.Fluent.Models;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

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

                var azureSubscription = new AzureSubscription(
                    azureSubscriptionSettings.SubscriptionId,
                    subscriptionName,
                    servicePrincipal,
                    azureSubscriptionSettings.Enabled,
                    locations);

                Subscriptions.Add(id, azureSubscription);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IAzureSubscription> AzureSubscriptions => Subscriptions.Values
            .OrderBy(item => item.SubscriptionId)
            .ToArray();

        private Dictionary<Guid, IAzureSubscription> Subscriptions { get; } = new Dictionary<Guid, IAzureSubscription>();

        private ISecretProvider SecretProvider { get; }

        private async Task<string> ResolveKeyvaultSecret(string clientSecretName)
        {
            await Task.CompletedTask;

            // TODO: Temporary hack: the secret identifier is just an environment variable name where we look up the secert.
            // TODO: Eventually we'll actually call keyvault to get the secrets.
            var secret = Environment.GetEnvironmentVariable(clientSecretName);
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException($"The environment variable '{clientSecretName}' is not set.");
            }

            return secret;
        }
    }
}
