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
        /// Gets Resume Environment Timeout In Seconds.
        /// </summary>
        public const int ResumeEnvironmentTimeoutInSeconds = 600;

        /// <summary>
        /// Gets Unavailable Environment Timeout In Seconds.
        /// </summary>
        public const int UnavailableEnvironmentTimeoutInSeconds = 60;
    }
}
