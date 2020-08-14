// <copyright file="IAzureSubscription.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents an Azure subscription that is available to the BackEnd.
    /// </summary>
    public interface IAzureSubscription
    {
        /// <summary>
        /// Gets the Azure subscription id.
        /// </summary>
        string SubscriptionId { get; }

        /// <summary>
        /// Gets the Azure subscription display name, not semantically relevant, but used for logging.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the service principal settings for accessing this Azure subscription.
        /// </summary>
        IServicePrincipal ServicePrincipal { get; }

        /// <summary>
        /// Gets a value indicating whether this Azure subscription is enabled for creating new resources.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// Gets the list of locations supported for this subscription.
        /// </summary>
        IEnumerable<AzureLocation> Locations { get; }

        /// <summary>
        /// Gets the compute quotas for this subscription.
        /// </summary>
        IReadOnlyDictionary<string, int> ComputeQuotas { get; }

        /// <summary>
        /// Gets the storage quotas for this subscription.
        /// </summary>
        IReadOnlyDictionary<string, int> StorageQuotas { get; }

        /// <summary>
        /// Gets the network quotas for this subscription.
        /// </summary>
        IReadOnlyDictionary<string, int> NetworkQuotas { get; }

        /// <summary>
        /// Gets or sets the service type supported by this Azure subscription.
        /// </summary>
        ServiceType? ServiceType { get; }
    }
}
