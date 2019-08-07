// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models
{
    /// <summary>
    /// Settings that are passed in to the service as config at runtime.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the git commit used to produce this build. Used for troubleshooting.
        /// </summary>
        public string GitCommit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the option which controls whether the
        /// system loads in memory data store for repositories to make inner loop faster.
        /// </summary>
        public bool UseMocksForLocalDevelopment { get; set; }
    }
}
