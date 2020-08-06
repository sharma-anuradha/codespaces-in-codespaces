// <copyright file="EnvironmentMonitorConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor
{
    /// <summary>
    /// Environment Monitor Constants.
    /// </summary>
    public static class EnvironmentMonitorConstants
    {
        /// <summary>
        /// Gets heartbeat timeout.
        /// </summary>
        public const int HeartbeatTimeoutInMinutes = 5;

        /// <summary>
        /// Gets buffer time.
        /// </summary>
        public const int BufferInMinutes = 1;

        /// <summary>
        /// Maximum Interval between heartbeats sent from VSO Agent.
        /// </summary>
        public const int HeartbeatIntervalInSeconds = 70;

        /// <summary>
        /// Suspend state monitor time for soft delete.
        /// </summary>
        public const int SoftDeleteStateMonitorTimeoutInSeconds = 15;

        /// <summary>
        /// Number of attempts before forced suspend for soft delete.
        /// </summary>
        public const int SoftDeleteStateMonitorMaxAttempts = 8;
    }
}
