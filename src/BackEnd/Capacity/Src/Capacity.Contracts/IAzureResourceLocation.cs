// <copyright file="IAzureResourceLocation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Specifies the location-coordinates of an azure resource.
    /// </summary>
    public interface IAzureResourceLocation
    {
        /// <summary>
        /// Gets the azure subscription.
        /// </summary>
        IAzureSubscription Subscription { get; }

        /// <summary>
        /// Gets the azure resource group.
        /// </summary>
        string ResourceGroup { get; }

        /// <summary>
        /// Gets the azure locations.
        /// </summary>
        AzureLocation Location { get; }
    }
}
