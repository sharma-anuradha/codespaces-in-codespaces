// <copyright file="CloudEnvironmentState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// The environment state.
    /// </summary>
    public enum CloudEnvironmentState
    {
        /// <summary>
        /// Uninitialized state.
        /// </summary>
        None = 0,

        /// <summary>
        /// Environment is created but not yet provisioning. (INITIAL STATE)
        /// </summary>
        Created,

        /// <summary>
        /// Readying the environment.
        /// </summary>
        Provisioning,

        /// <summary>
        /// Environment is ready and available to connect.
        /// </summary>
        Available,

        /// <summary>
        /// Environment is ready but waiting for the host to connect.
        /// </summary>
        Awaiting,

        /// <summary>
        /// Environment is unavailable connect. There is no recovery path.
        /// </summary>
        Unavailable,

        /// <summary>
        /// Environment is deleted. (TERMINAL STATE)
        /// </summary>
        Deleted,

        /// <summary>
        /// Environment is shutdown.
        /// </summary>
        Shutdown,

        /// <summary>
        /// Environment is starting.
        /// </summary>
        Starting,

        /// <summary>
        /// Environment is detaching storage in preparation for shutdown.
        /// </summary>
        ShuttingDown,
    }
}
