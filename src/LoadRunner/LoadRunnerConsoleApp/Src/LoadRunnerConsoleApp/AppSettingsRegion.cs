// <copyright file="AppSettingsRegion.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

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
        public string AuthAccountId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this region is enabled or not.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the total environement count that is needed for the region.
        /// </summary>
        public int TotalEnvironementRunCount { get; set; }
    }
}
