// <copyright file="AzureSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    [DebuggerDisplay("{DisplayName} ({SubscriptionId})")]
    public class AzureSubscription : IAzureSubscription
    {
        private static readonly Lazy<ReadOnlyDictionary<string, int>> EmptyQuotas = new Lazy<ReadOnlyDictionary<string, int>>(() => new ReadOnlyDictionary<string, int>(new Dictionary<string, int>()));

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureSubscription"/> class.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="displayName">The display name for logging.</param>
        /// <param name="servicePrincipal">The service principal for accessing this subscription.</param>
        /// <param name="enabled">A value that indicates whether this subscrdiption is enabled for new resources.</param>
        /// <param name="locations">The list of Azure locations supported for this subscription.</param>
        /// <param name="computeQuotas">The compute quotas.</param>
        /// <param name="storageQuotas">The storage quotas.</param>
        /// <param name="networkQuotas">The network quotas.</param>
        public AzureSubscription(
            string subscriptionId,
            string displayName,
            IServicePrincipal servicePrincipal,
            bool enabled,
            IReadOnlyCollection<AzureLocation> locations,
            IReadOnlyDictionary<string, int> computeQuotas,
            IReadOnlyDictionary<string, int> storageQuotas,
            IReadOnlyDictionary<string, int> networkQuotas)
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
            ComputeQuotas = computeQuotas ?? EmptyQuotas.Value;
            StorageQuotas = storageQuotas ?? EmptyQuotas.Value;
            NetworkQuotas = networkQuotas ?? EmptyQuotas.Value;
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

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> ComputeQuotas { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> StorageQuotas { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, int> NetworkQuotas { get; }
    }
}
