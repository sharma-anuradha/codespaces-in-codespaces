// <copyright file="IAzureSubscriptionCatalog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// The catalog of Azure subscriptions that are available to the BackEnd.
    /// </summary>
    public interface IAzureSubscriptionCatalog
    {
        /// <summary>
        /// Gets the set of configured Azure subscriptions.
        /// </summary>
        IEnumerable<IAzureSubscription> AzureSubscriptions { get; }
    }
}
