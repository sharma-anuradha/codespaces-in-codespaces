// <copyright file="ResourceComponent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Resource component building blocks.
    /// </summary>
    public class ResourceComponent : IEquatable<ResourceComponent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceComponent"/> class.
        /// </summary>
        public ResourceComponent()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceComponent"/> class.
        /// </summary>
        /// <param name="componentType">Component type.</param>
        /// <param name="azureResourceInfo">Azure resource info.</param>
        /// <param name="resourceRecordId">Resource record id.</param>
        public ResourceComponent(ComponentType componentType, AzureResourceInfo azureResourceInfo, string resourceRecordId = null)
        {
            ComponentType = componentType;
            AzureResourceInfo = azureResourceInfo;
            ResourceRecordId = resourceRecordId;
        }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "componentType")]
        public ComponentType ComponentType { get; set; }

        /// <summary>
        /// Gets or sets component resource info.
        /// </summary>
        [JsonProperty(PropertyName = "azureResourceInfo")]
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the resource record id.
        /// Note: this is optional and is only required if there is a backing back-end resource in the database.
        /// </summary>
        public string ResourceRecordId { get; set; }

        /// <inheritdoc/>
        public bool Equals(ResourceComponent other)
        {
            if (other is null)
            {
                return false;
            }

            if (ComponentType != other.ComponentType)
            {
                return false;
            }

            if (!string.Equals(ResourceRecordId, other.ResourceRecordId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (AzureResourceInfo != default && !AzureResourceInfo.Equals(other.AzureResourceInfo))
            {
                return false;
            }

            return true;
        }
    }
}