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

        /// <summary>
        /// Gets or sets a value indicating whether to queue the resource allocation.
        /// </summary>
        public bool QueueResourceAllocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to shallow clone first, then fetch and build devcontainer concurrently.
        /// </summary>
        public bool ShallowClone { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to add the git credential helper with local or system scope.
        /// </summary>
        public bool LocalCredentialHelper { get; set; }
    }
}
