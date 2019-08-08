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
        /// Gets or sets a value indicating whether to create a file share for the environment.
        /// </summary>
        public bool CreateFileShare { get; set; }
    }
}
