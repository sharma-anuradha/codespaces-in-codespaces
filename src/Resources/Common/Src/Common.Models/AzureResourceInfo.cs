// <copyright file="AzureResourceInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models.Extensions;
using Newtonsoft.Json;

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
            string subscriptionId,
            string resourceGroup,
            string name)
            : this(Guid.Parse(subscriptionId), resourceGroup, name)
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
        /// Gets or sets the subscription id.
        /// </summary>
        [JsonProperty(PropertyName = "subscriptionId")]
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the resource group name.
        /// </summary>
        [JsonProperty(PropertyName = "resourceGroup")]
        public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the resource name.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets properties which are not common across all resources.
        /// </summary>
        [JsonProperty(PropertyName = "properties")]
        public IDictionary<string, string> Properties { get; set; }

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

            if (!Properties.DictionaryEquals(other.Properties))
            {
                return false;
            }

            return true;
        }
    }
}
