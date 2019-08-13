// <copyright file="ScalingInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models
{
    /// <summary>
    /// Defines the scaling input that is used to update the target size of a given pool.
    /// </summary>
    public class ScalingInput
    {
        /// <summary>
        /// Gets or sets the Target Count that this resource should be maintained at when pooled.
        /// </summary>
        public int TargetCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the sku.
        /// </summary>
        public string SkuName { get; set; }

        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        public ResourceType Type { get; set; }

        /// <summary>
        /// Gets or sets the location of the resource.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets Environment Skus that use this Resource Unit.
        /// </summary>
        public IEnumerable<string> EnvironmentSkus { get; set; }
    }
}
