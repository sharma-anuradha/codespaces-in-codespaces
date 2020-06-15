// <copyright file="IAzureResourceGroup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// Specifies the azure resource group.
    /// </summary>
    public interface IAzureResourceGroup
    {
        /// <summary>
        /// Gets the azure subscription.
        /// </summary>
        IAzureSubscription Subscription { get; }

        /// <summary>
        /// Gets the azure resource group.
        /// </summary>
        string ResourceGroup { get; }
    }
}
