// <copyright file="CloudEnvironmentOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment registration options.
    /// </summary>
    public class CloudEnvironmentOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use custom containers for this environment.
        /// </summary>
        public bool CustomContainers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the new terminal output for this environment.
        /// </summary>
        public bool NewTerminal { get; set; }
    }
}
