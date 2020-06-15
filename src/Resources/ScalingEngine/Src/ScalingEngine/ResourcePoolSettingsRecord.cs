// <copyright file="ResourcePoolSettingsRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ScalingEngine
{
    /// <summary>
    /// Defines the a given resource pools current state.
    /// </summary>
    public class ResourcePoolSettingsRecord : TaggedEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether the pool should be enabled.
        /// </summary>
        [JsonProperty(PropertyName = "isEnabled")]
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the target count.
        /// </summary>
        [JsonProperty(PropertyName = "targetCount")]
        public int? TargetCount { get; set; }
    }
}
