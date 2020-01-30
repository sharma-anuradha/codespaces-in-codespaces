// <copyright file="AppSettingsRegionPool.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Regional app settings.
    /// </summary>
    public class AppSettingsRegionPool
    {
        /// <summary>
        /// Gets or sets the pool id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the pool target count.
        /// </summary>
        public int? TargetCount { get; set; }
    }
}