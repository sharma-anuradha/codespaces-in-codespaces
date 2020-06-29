// <copyright file="ComputeUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Plans;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Compute usage details.
    /// </summary>
    public class ComputeUsageDetail : IEquatable<ComputeUsageDetail>
    {
        /// <summary>
        /// Gets or sets the compute time in seconds.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "usage")]
        public double Usage { get; set; }

        /// <summary>
        /// Gets or sets the sku to which the compute resource is apart.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "sku")]
        public string Sku { get; set; }

        /// <summary>
        /// Compare two instances for equality.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(ComputeUsageDetail other)
        {
            if (other == null)
            {
                return false;
            }

            return Usage == other.Usage && Sku == other.Sku;
        }
    }
}
