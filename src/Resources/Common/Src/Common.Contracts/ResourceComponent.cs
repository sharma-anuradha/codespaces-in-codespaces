// <copyright file="ResourceComponent.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
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
        /// <param name="preserve">preserve component or not on resource deletion.</param>
        /// <param name="componentId">component id.</param>
        public ResourceComponent(
            ResourceType componentType,
            AzureResourceInfo azureResourceInfo,
            string resourceRecordId = null,
            bool preserve = false,
            string componentId = default)
        {
            ComponentType = componentType;
            AzureResourceInfo = azureResourceInfo;
            ResourceRecordId = resourceRecordId;
            Preserve = preserve;
            ComponentId = string.IsNullOrEmpty(componentId) ? Guid.NewGuid().ToString() : componentId;
        }

        /// <summary>
        /// Gets or sets the component type.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "componentType")]
        public ResourceType ComponentType { get; set; }

        /// <summary>
        /// Gets or sets component resource info.
        /// </summary>
        [JsonProperty(PropertyName = "azureResourceInfo")]
        public AzureResourceInfo AzureResourceInfo { get; set; }

        /// <summary>
        /// Gets or sets the resource record id.
        /// Note: this is optional and is only required if there is a backing back-end resource in the database.
        /// </summary>
        [JsonProperty(PropertyName = "resourceRecordId")]
        public string ResourceRecordId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether delete component with resource or not.
        /// </summary>
        [JsonProperty(PropertyName = "preserve")]
        public bool Preserve { get; set; }

        /// <summary>
        /// Gets or sets component Id.
        /// </summary>
        [JsonProperty(PropertyName = "componentId")]
        public string ComponentId { get; set; }

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

            if (AzureResourceInfo == default && other.AzureResourceInfo != default)
            {
                return false;
            }

            return true;
        }
    }
}