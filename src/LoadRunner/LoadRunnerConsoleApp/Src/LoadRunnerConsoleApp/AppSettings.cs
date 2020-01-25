// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Security;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// App settings object for the test runner.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets the current git commit.
        /// </summary>
        public string GitCommit { get; set; }

        /// <summary>
        /// Gets or sets the base environment uri.
        /// </summary>
        public string EnvironmentsBaseUri { get; set; }

        /// <summary>
        /// Gets or sets the git repositories that we should use.
        /// </summary>
        public IEnumerable<string> GitRepositories { get; set; }

        /// <summary>
        /// Gets or sets the auth tenant.
        /// </summary>
        public Guid AuthTenant { get; set; }

        /// <summary>
        /// Gets or sets the auth client id.
        /// </summary>
        public string AuthClientId { get; set; }

        /// <summary>
        /// Gets or sets the auth client secret.
        /// </summary>
        public string AuthClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should be using the
        /// developers personal stamp.
        /// </summary>
        public bool DeveloperPersonalStamp { get; set; }

        /// <summary>
        /// Gets or sets the auth audience.
        /// </summary>
        public string AuthAudience { get; set; }

        /// <summary>
        /// Gets or sets the pool target count.
        /// </summary>
        public int? DefaultTargetCount { get; set; }

        /// <summary>
        /// Gets or sets the total batch runs that we want to run.
        /// </summary>
        public int? BatchTotalRuns { get; set; }

        /// <summary>
        /// Gets or sets the amount of time we want to pause execution between when
        /// an environment was created and when its torn down in minutes.
        /// </summary>
        public int? PauseExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the regions that we want to work through.
        /// </summary>
        public IDictionary<string, AppSettingsRegion> Regions { get; set; }

        /// <summary>
        /// Gets or sets the database that we want to work with.
        /// </summary>
        public AppSettingsDatabase Database { get; set; }
    }
}
