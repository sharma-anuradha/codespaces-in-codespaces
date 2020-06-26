// <copyright file="OutOfCapacityException.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Out Of Capacity Exception.
    /// </summary>
    public class OutOfCapacityException : ResourceBrokerException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OutOfCapacityException"/> class.
        /// </summary>
        /// <param name="skuName">Target Sku Name.</param>
        /// <param name="type">Target Type.</param>
        /// <param name="location">Target Location.</param>
        public OutOfCapacityException(string skuName, ResourceType type, string location)
            : base($"Pool is currently empty. SkuName = {skuName}, Type = {type}, Location = {location}")
        {
            SkuName = skuName;
            Type = type;
            Location = location;
        }

        /// <summary>
        /// Gets or sets Sku Name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets Type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets Location.
        /// </summary>
        public string Location { get; set; }
    }
}
