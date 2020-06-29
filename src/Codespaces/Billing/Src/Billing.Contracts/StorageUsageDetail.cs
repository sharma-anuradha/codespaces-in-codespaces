// <copyright file="StorageUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Storage usage details.
    /// </summary>
    public class StorageUsageDetail : IEquatable<StorageUsageDetail>
    {
        /// <summary>
        /// Gets or sets storage time in milliseconds.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "usage")]
        public double Usage { get; set; }

        /// <summary>
        /// Gets or sets storage size in GB.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "size")]
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the sku to which the storage resource is apart.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public string Sku { get; set; }

               /// <summary>
        /// Compare two instances for equality.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(StorageUsageDetail other)
        {
            if (other == null)
            {
                return false;
            }

            return Usage == other.Usage && Sku == other.Sku && Size == other.Size;
        }
    }
}
