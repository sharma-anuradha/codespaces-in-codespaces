// <copyright file="AzureSubscriptionCatalogExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// <see cref="IAzureSubscriptionCatalog"/> extensions.
    /// </summary>
    public static class AzureSubscriptionCatalogExtensions
    {
        /// <summary>
        /// Gets all data-plane infrastructure and data subscriptions.
        /// </summary>
        /// <param name="azureSubscriptionCatalog">The Azure subscription catalog.</param>
        /// <returns>The list of <see cref="IAzureSubscription"/>.</returns>
        public static IEnumerable<IAzureSubscription> AzureSubscriptionsIncludingInfrastructure(this IAzureSubscriptionCatalog azureSubscriptionCatalog)
        {
            Requires.NotNull(azureSubscriptionCatalog, nameof(azureSubscriptionCatalog));
            return azureSubscriptionCatalog.AzureSubscriptions.Union(new[] { azureSubscriptionCatalog.InfrastructureSubscription });
        }
    }
}
