// <copyright file="AzureResourceGroup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <inheritdoc/>
    public class AzureResourceGroup : IAzureResourceGroup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceGroup"/> class.
        /// </summary>
        /// <param name="azureSubscription">The azure subscription.</param>
        /// <param name="resourceGroup">The azure resource group name.</param>
        public AzureResourceGroup(
            IAzureSubscription azureSubscription,
            string resourceGroup)
        {
            Requires.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Subscription = Requires.NotNull(azureSubscription, nameof(azureSubscription));
            ResourceGroup = resourceGroup;
        }

        /// <inheritdoc/>
        public IAzureSubscription Subscription { get; }

        /// <inheritdoc/>
        public string ResourceGroup { get; }
    }
}
