// <copyright file="CapacityRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Diagnostics;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts
{
    /// <summary>
    /// A capacity record for a subscription and quota type.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [DebuggerDisplay("Id = {Id}")]
    public class CapacityRecord : TaggedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityRecord"/> class.
        /// </summary>
        public CapacityRecord()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityRecord"/> class.
        /// </summary>
        /// <param name="usage">The <see cref="AzureResourceUsage"/> instance.</param>
        public CapacityRecord(AzureResourceUsage usage)
            : this(
                  MakeId(usage),
                  usage.SubscriptionId,
                  usage.ServiceType,
                  usage.Location,
                  usage.Quota,
                  usage.Limit,
                  usage.CurrentValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacityRecord"/> class.
        /// </summary>
        /// <param name="id">The record id. Null to compute the compsite key.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="serviceType">The capacity service type.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="quota">The quota name.</param>
        /// <param name="limit">The quota limit for this subscription/location.</param>
        /// <param name="currentValue">The quota current value for this subscription/location.</param>
        public CapacityRecord(
            string id,
            string subscriptionId,
            ServiceType serviceType,
            AzureLocation location,
            string quota,
            long limit,
            long currentValue)
        {
            Id = id ?? MakeId(subscriptionId, serviceType, location, quota);
            SubscriptionId = subscriptionId;
            ServiceType = serviceType;
            Location = location;
            Quota = quota;
            Limit = limit;
            CurrentValue = currentValue;
        }

        /// <summary>
        /// Gets or sets the subscription id.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the azure location.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public AzureLocation Location { get; set; }

        /// <summary>
        /// Gets or sets the capacity service type.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ServiceType ServiceType { get; set; }

        /// <summary>
        /// Gets or sets the quota name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Quota { get; set; }

        /// <summary>
        /// Gets or sets the quota limit for this subscription/location.
        /// </summary>
        [JsonProperty]
        public long Limit { get; set; }

        /// <summary>
        /// Gets or sets the quota current value for this subscription/location.
        /// </summary>
        [JsonProperty]
        public long CurrentValue { get; set; }

        /// <summary>
        /// Create a composite key from subscription, service type, location, and quota.
        /// </summary>
        /// <param name="usage">The <see cref="AzureResourceUsage"/> instance.</param>
        /// <returns>The composite id.</returns>
        public static string MakeId(AzureResourceUsage usage)
        {
            return MakeId(usage.SubscriptionId, usage.ServiceType, usage.Location, usage.Quota);
        }

        /// <summary>
        /// Create a composite key from subscription, service type, location, and quota.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <param name="serviceType">The capacity service type.</param>
        /// <param name="location">The azure location.</param>
        /// <param name="quota">The quota name.</param>
        /// <returns>The composite id.</returns>
        public static string MakeId(string subscriptionId, ServiceType serviceType, AzureLocation location, string quota)
        {
            return $"{subscriptionId}-{serviceType}-{location}-{quota}".ToLowerInvariant();
        }
    }
}
