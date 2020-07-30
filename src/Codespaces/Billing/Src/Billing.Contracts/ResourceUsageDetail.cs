// <copyright file="ResourceUsageDetail.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Billing
{
    /// <summary>
    /// Compute and Storage usage details..
    /// </summary>
    public class ResourceUsageDetail : IEquatable<ResourceUsageDetail>
    {
        /// <summary>
        /// Gets or sets compute usage.
        /// </summary>
        [JsonProperty(Required = Required.Default, PropertyName = "compute", NullValueHandling = NullValueHandling.Ignore)]
        public IList<ComputeUsageDetail> Compute { get; set; }

        /// <summary>
        /// Gets or sets storage usage.
        /// </summary>
        [JsonProperty(Required = Required.Always, PropertyName = "storage")]
        public IList<StorageUsageDetail> Storage { get; set; }

        /// <summary>
        /// Test for equality.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>True if the ResourceUsageDetail are effectivly equal.</returns>
        public bool Equals(ResourceUsageDetail other)
        {
            var hasCompute = Compute != null;
            var otherHasComputer = other.Compute != null;
            if (hasCompute != otherHasComputer)
            {
                return false;
            }

            if (hasCompute && !Compute.OrderBy(o => o.Sku)
                .SequenceEqual(other.Compute.OrderBy(o => o.Sku)))
            {
                return false;
            }

            var hasStorage = Storage != null;
            var otherHasStorage = other.Storage != null;
            if (hasStorage != otherHasStorage)
            {
                return false;
            }

            if (hasStorage && !Storage.OrderBy(o => o.Sku)
                .SequenceEqual(other.Storage.OrderBy(o => o.Sku)))
            {
                return false;
            }

            return true;
        }
    }
}
