// <copyright file="SystemResourceDimensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts
{
    /// <summary>
    /// Aggregate dimensions over system resources.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class SystemResourceDimensions
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the SKU name.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the Azure location.
        /// </summary>
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the resource is ready.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the resource is assigned.
        /// </summary>
        public bool IsAssigned { get; set; }

        /// <summary>
        /// Gets or sets the pool reference code.
        /// </summary>
        public string PoolReferenceCode { get; set; }

        /// <summary>
        /// Gets or sets the subscription Id.
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, SkuName, Location, IsReady, IsAssigned, PoolReferenceCode, SubscriptionId);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is SystemResourceDimensions other))
            {
                return false;
            }

            return this.Type == other.Type
                && this.SkuName == other.SkuName
                && this.Location == other.Location
                && this.IsReady == other.IsReady
                && this.IsAssigned == other.IsAssigned
                && this.PoolReferenceCode == other.PoolReferenceCode
                && this.SubscriptionId == other.SubscriptionId;
        }
    }
}
