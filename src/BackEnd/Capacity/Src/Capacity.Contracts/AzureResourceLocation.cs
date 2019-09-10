// <copyright file="AzureResourceLocation.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <inheritdoc/>
    public class AzureResourceLocation : IAzureResourceLocation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceLocation"/> class.
        /// </summary>
        /// <param name="azureSubscription">The azure subscription.</param>
        /// <param name="resourceGroup">The azure resource group name.</param>
        /// <param name="location">The azure location.</param>
        public AzureResourceLocation(
            IAzureSubscription azureSubscription,
            string resourceGroup,
            AzureLocation location)
        {
            Requires.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Subscription = Requires.NotNull(azureSubscription, nameof(azureSubscription));
            Location = location;
            ResourceGroup = resourceGroup;
        }

        /// <inheritdoc/>
        public IAzureSubscription Subscription { get; }

        /// <inheritdoc/>
        public string ResourceGroup { get; }

        /// <inheritdoc/>
        public AzureLocation Location { get; }
    }
}
