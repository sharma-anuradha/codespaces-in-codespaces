// <copyright file="DeveloperSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEnd.Common
{
    /// <summary>
    /// Developer settings class.
    /// </summary>
    public class DeveloperSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeveloperSettings"/> class.
        /// </summary>
        /// <param name="enabled">True to enable developer settings.</param>
        /// <param name="host">Host for url requests. For example, uses ngrok to forward requests to your local machine from Azure VM.</param>
        public DeveloperSettings(bool enabled, string host)
        {
            Enabled = true;
            ForwarderHost = host;
        }

        /// <summary>
        /// Gets a value indicating whether if developer settings are enabled.
        /// </summary>
        public bool Enabled { get; } = false;

        /// <summary>
        /// Gets a value for the forwarder host.
        /// </summary>
        public string ForwarderHost { get; }
    }
}
