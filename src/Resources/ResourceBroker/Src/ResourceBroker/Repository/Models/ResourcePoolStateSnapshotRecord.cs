// <copyright file="ResourcePoolStateSnapshotRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models
{
    /// <summary>
    /// Defines a given resource pool's current state.
    /// </summary>
    public class ResourcePoolStateSnapshotRecord : TaggedEntity
    {
        /// <summary>
        /// Gets or sets a value indicating whether the pool is at level.
        /// </summary>
        [JsonProperty(PropertyName = "isAtTargetCount")]
        public bool IsAtTargetCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool has ready items at level.
        /// </summary>
        [JsonProperty(PropertyName = "isReadyAtTargetCount")]
        public bool IsReadyAtTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the current version code.
        /// </summary>
        [JsonProperty(PropertyName = "versionCode")]
        public string VersionCode { get; set; }

        /// <summary>
        /// Gets or sets the current current Target Count.
        /// </summary>
        [JsonProperty(PropertyName = "targetCount")]
        public int TargetCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned items.
        /// </summary>
        [JsonProperty(PropertyName = "unassignedCount")]
        public int UnassignedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned item that are the current version.
        /// </summary>
        [JsonProperty(PropertyName = "unassignedVersionCount")]
        public int UnassignedVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned item that aren't the current version.
        /// </summary>
        [JsonProperty(PropertyName = "unassignedNotVersionCount")]
        public int UnassignedNotVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned items.
        /// </summary>
        [JsonProperty(PropertyName = "readyUnassignedCount")]
        public int ReadyUnassignedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned item that are the current version.
        /// </summary>
        [JsonProperty(PropertyName = "readyUnassignedVersionCount")]
        public int ReadyUnassignedVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned item that aren't the current version.
        /// </summary>
        [JsonProperty(PropertyName = "readyUnassignedNotVersionCount")]
        public int ReadyUnassignedNotVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned item that aren't the current version.
        /// </summary>
        [JsonProperty(PropertyName = "pendingRquestCount")]
        public int PendingRquestCount { get; set; }

        /// <summary>
        /// Gets or sets the current pool Dimensions.
        /// </summary>
        [JsonProperty(PropertyName = "dimensions")]
        public IDictionary<string, string> Dimensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool is enabled or not.
        /// </summary>
        [JsonProperty(PropertyName = "isEnabled")]
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the time the pool was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "updated")]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Gets or sets the override is enabled.
        /// </summary>
        public bool? OverrideIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the override target count.
        /// </summary>
        public int? OverrideTargetCount { get; set; }
    }
}
