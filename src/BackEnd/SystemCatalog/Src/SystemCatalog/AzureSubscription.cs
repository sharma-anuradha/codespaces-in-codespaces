// <copyright file="AzureSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog
{
    /// <inheritdoc/>
    public class AzureSubscription : IAzureSubscription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscription"/> class.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="displayName">The display name for logging.</param>
        /// <param name="servicePrincipal">The service principal for accessing this subscription.</param>
        /// <param name="enabled">A value that indicates whether this subscrdiption is enabled for new resources.</param>
        /// <param name="locations">The list of Azure locations supported for this subscription.</param>
        public AzureSubscription(
            string subscriptionId,
            string displayName,
            IServicePrincipal servicePrincipal,
            bool enabled,
            IReadOnlyCollection<AzureLocation> locations)
        {
            Requires.NotNullOrEmpty(subscriptionId, nameof(subscriptionId));
            Requires.NotNullOrEmpty(displayName, nameof(displayName));
            Requires.NotNull(servicePrincipal, nameof(servicePrincipal));
            Requires.NotNullOrEmpty(locations, nameof(locations));

            SubscriptionId = subscriptionId;
            DisplayName = displayName;
            ServicePrincipal = servicePrincipal;
            Enabled = enabled;
            Locations = locations;
        }

        /// <inheritdoc/>
        public string SubscriptionId { get; }

        /// <inheritdoc/>
        public string DisplayName { get; }

        /// <inheritdoc/>
        public IServicePrincipal ServicePrincipal { get; }

        /// <inheritdoc/>
        public bool Enabled { get; }

        /// <inheritdoc/>
        public IEnumerable<AzureLocation> Locations { get; }
    }
}
