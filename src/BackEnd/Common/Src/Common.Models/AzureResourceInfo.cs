// <copyright file="AzureResourceInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Info about an azure resource.
    /// </summary>
    public class AzureResourceInfo : IEquatable<AzureResourceInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfo"/> class.
        /// </summary>
        public AzureResourceInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureResourceInfo"/> class.
        /// </summary>
        /// <param name="subscriptionId">The azure subscription id.</param>
        /// <param name="resourceGroup">The azure resource group.</param>
        /// <param name="name">The resource name.</param>
        public AzureResourceInfo(
            Guid subscriptionId,
            string resourceGroup,
            string name)
        {
            Requires.NotEmpty(subscriptionId, nameof(subscriptionId));
            Requires.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Requires.NotNullOrEmpty(name, nameof(name));

            SubscriptionId = subscriptionId;
            ResourceGroup = resourceGroup;
            Name = name;
        }

        /// <summary>
        /// Gets the subscription id.
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets the resource group name.
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string Name { get; set; }

        /// <inheritdoc/>
        public bool Equals(AzureResourceInfo other)
        {
            if (other is null)
            {
                return false;
            }

            if (!SubscriptionId.Equals(other.SubscriptionId))
            {
                return false;
            }

            if (!ResourceGroup.Equals(other.ResourceGroup, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
