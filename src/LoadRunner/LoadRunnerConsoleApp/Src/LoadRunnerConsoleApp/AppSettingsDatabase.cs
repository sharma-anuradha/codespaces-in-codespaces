// <copyright file="AppSettingsDatabase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp
{
    /// <summary>
    /// Database App Settings.
    /// </summary>
    public class AppSettingsDatabase
    {
        /// <summary>
        /// Gets or sets the preferred location for the database.
        /// </summary>
        public string PreferredLocation { get; set; }

        /// <summary>
        /// Gets or sets the id for the database.
        /// </summary>
        public string DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the host url for the database.
        /// </summary>
        public string HostUrl { get; set; }

        /// <summary>
        /// Gets or sets the auth key for the database.
        /// </summary>
        public string AuthKey { get; set; }
    }
}
