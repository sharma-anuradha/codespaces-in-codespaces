// <copyright file="CloudEnvironmentStateUpdateTriggers.cs" company="Microsoft">
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
        /// Environment Monitor triggered state change.
        /// </summary>
        public const string EnvironmentMonitor = "EnvironmentMonitor";

        /// <summary>
        /// Start environment failed.
        /// </summary>
        public const string ForceEnvironmentShutdown = "ForceEnvironmentShutdown";

        /// <summary>
        /// Start environment failed.
        /// </summary>
        public const string StartEnvironmentJobFailed = "StartEnvironmentJobFailed";

        /// <summary>
        /// Export environment failed.
        /// </summary>
        public const string ExportEnvironmentJobFailed = "ExportEnvironmentJobFailed";

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
        /// Hard Delete environment.
        /// </summary>
        public const string HardDeleteEnvironment = "HardDeleteEnvironment";

        /// <summary>
        /// Soft Delete environment.
        /// Export environment.
        /// </summary>
        public const string ExportEnvironment = "ExportEnvironment";

        /// <summary>
        /// Start environment.
        /// </summary>
        public const string SoftDeleteEnvironment = "SoftDeleteEnvironment";

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
