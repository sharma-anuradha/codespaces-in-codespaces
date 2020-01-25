// <copyright file="AppSettingsRegion.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Regional app settings.
    /// </summary>
    public class AppSettingsRegion
    {
        /// <summary>
        /// Gets or sets the auth account id that should be used.
        /// </summary>
        public string AccountPlanId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this region is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the total environement count that is needed for the region.
        /// </summary>
        public int TotalEnvironementRunCount { get; set; }

        /// <summary>
        /// Gets or sets the auth account id that should be used.
        /// </summary>
        public IEnumerable<AppSettingsRegionPool> TargetPools { get; set; }

        /// <summary>
        /// Gets or sets the pool target count.
        /// </summary>
        public int? DefaultTargetCount { get; set; }
    }
}