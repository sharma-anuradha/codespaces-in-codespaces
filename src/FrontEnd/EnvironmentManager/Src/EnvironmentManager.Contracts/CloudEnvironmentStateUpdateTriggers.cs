// <copyright file="CloudEnvironmentStateUpdateReasons.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Reasons for cloud environment state changes.
    /// </summary>
    public static class CloudEnvironmentStateUpdateTriggers
    {
        /// <summary>
        /// Start environment failed.
        /// </summary>
        public const string ForceEnvironmentShutdown = "ForceEnvironmentShutdown";

        /// <summary>
        /// Start environment failed.
        /// </summary>
        public const string StartEnvironmentJobFailed = "StartEnvironmentJobFailed";

        /// <summary>
        /// Create new environment.
        /// </summary>
        public const string CreateEnvironment = "CreateEnvironment";

        /// <summary>
        /// Get environemnt details.
        /// </summary>
        public const string GetEnvironment = "GetEnvironment";

        /// <summary>
        /// Shutdown environment.
        /// </summary>
        public const string ShutdownEnvironment = "ShutdownEnvironment";

        /// <summary>
        /// Start environment.
        /// </summary>
        public const string StartEnvironment = "StartEnvironment";

        /// <summary>
        /// Start environment.
        /// </summary>
        public const string DeleteEnvironment = "DeleteEnvironment";

        /// <summary>
        /// Heartbeat triggered state change.
        /// </summary>
        public const string Heartbeat = "Heartbeat";

        /// <summary>
        /// Callback triggered state change.
        /// </summary>
        public const string EnvironmentCallback = "EnvironmentCallback";

        /// <summary>
        /// Environment settings were updated.
        /// </summary>
        public const string EnvironmentSettingsChanged = "EnvironmentSettingsChanged";
    }
}
