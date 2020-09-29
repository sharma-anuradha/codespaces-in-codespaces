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

        /// <summary>
        /// No heartbeat.
        /// </summary>
        public const string NoHeartbeatReason = "NoHeartbeat";

        /// <summary>
        /// Unhealthy unavailable heartbeat.
        /// </summary>
        public const string UnhealthyUnavailableHeartbeatReason = "UnhealthyUnavailableHeartbeat";

        /// <summary>
        /// Environment unavailable.
        /// </summary>
        public const string EnvironmentUnavailableReason = "EnvironmentUnavailable";

        /// <summary>
        /// No heartbeat marking unavailable.
        /// </summary>
        public const string NoHeartbeatMarkingUnavailableReason = "NoHeartbeatMarkingUnavailable";

        /// <summary>
        /// No heartbeat for unavailable environment.
        /// </summary>
        public const string NoHeartbeatForUnavailableEnvironmentReason = "NoHeartbeatForUnavailableEnvironment";

        /// <summary>
        /// Healthy heartbeat.
        /// </summary>
        public const string HealthyHeartbeatReason = "HealthyHeartbeat";

        /// <summary>
        /// Stop monitoring heartbeat state.
        /// </summary>
        public const string StopHeartbeatMonitoringStateReason = "StopHeartbeatMonitoringState";

        /// <summary>
        /// Healthy state transition.
        /// </summary>
        public const string HealthyStateTransitionReason = "HealthyStateTransition";

        /// <summary>
        /// Unknown state transition.
        /// </summary>
        public const string UnknownStateTransitionReason = "UnknownStateTransition";

        /// <summary>
        /// Timeout in state transition.
        /// </summary>
        public const string TimeoutInStateTransitionReason = "TimeoutInStateTransition";

        /// <summary>
        /// Healthy intermediate state transition.
        /// </summary>
        public const string HealthyIntermediateStateTransitionReason = "HealthyIntermediateStateTransition";
    }
}
