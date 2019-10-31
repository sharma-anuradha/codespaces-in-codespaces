// <copyright file="ResourcePoolStateSnapshotRecord.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using Microsoft.VsSaaS.Common.Models;

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
        public bool IsAtTargetCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool has ready items at level.
        /// </summary>
        public bool IsReadyAtTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the current version code.
        /// </summary>
        public string VersionCode { get; set; }

        /// <summary>
        /// Gets or sets the current current Target Count.
        /// </summary>
        public int TargetCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned items.
        /// </summary>
        public int UnassignedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned item that are the current version.
        /// </summary>
        public int UnassignedVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of unassigned item that aren't the current version.
        /// </summary>
        public int UnassignedNotVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned items.
        /// </summary>
        public int ReadyUnassignedCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned item that are the current version.
        /// </summary>
        public int ReadyUnassignedVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the count of ready unassigned item that aren't the current version.
        /// </summary>
        public int ReadyUnassignedNotVersionCount { get; set; }

        /// <summary>
        /// Gets or sets the current pool Dimensions.
        /// </summary>
        public IDictionary<string, string> Dimensions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the pool is enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the time the pool was last updated.
        /// </summary>
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
