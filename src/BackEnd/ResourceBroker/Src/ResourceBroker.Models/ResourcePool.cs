// <copyright file="ResourcePool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Defines the scaling input that is used to update the target size of a given pool.
    /// </summary>
    public class ResourcePool
    {
        private int targetCount;
        private bool isEnabled = true;

        /// <summary>
        /// Gets or sets the Target Count that this resource should be maintained at when pooled.
        /// </summary>
        public int TargetCount
        {
            get { return OverrideTargetCount.HasValue ? OverrideTargetCount.Value : targetCount; }
            set { targetCount = value; }
        }

        /// <summary>
        /// Gets or sets the Override Target Count that overrides the Target count.
        /// </summary>
        public int? OverrideTargetCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get { return OverrideIsEnabled.HasValue ? OverrideIsEnabled.Value : isEnabled; }
            set { isEnabled = value; }
        }

        /// <summary>
        /// Gets or sets the Override Is Enabled that overrides the Is Enabled value.
        /// </summary>
        public bool? OverrideIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the additional resource details that identiy the resource.
        /// </summary>
        public ResourcePoolResourceDetails Details { get; set; }

        /// <summary>
        /// Gets or sets the Frontend Environment/Plan Skus that use this Resource Unit.
        /// </summary>
        public IEnumerable<string> LogicalSkus { get; set; }

        /// <summary>
        /// Gets the Max Create Batch Count.
        /// </summary>
        public int MaxCreateBatchCount
        {
            get { return 25; }
        }

        /// <summary>
        /// Gets the Max Delete Batch Count.
        /// </summary>
        public int MaxDeleteBatchCount
        {
            get { return 35; }
        }
    }
}
