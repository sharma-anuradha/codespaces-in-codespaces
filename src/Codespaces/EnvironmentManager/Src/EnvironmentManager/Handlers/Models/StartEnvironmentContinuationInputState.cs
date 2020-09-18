// <copyright file="StartEnvironmentContinuationInputState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Start environment continuation state.
    /// </summary>
    public enum StartEnvironmentContinuationInputState
    {
        /// <summary>
        /// Get existing resources.
        /// </summary>
        GetResource = 0,

        /// <summary>
        /// Allocate resource.
        /// </summary>
        AllocateResource = 1,

        /// <summary>
        /// Check Resource State.
        /// </summary>
        CheckResourceState = 2,

        /// <summary>
        /// Kick off start compute.
        /// </summary>
        StartCompute = 3,

        /// <summary>
        /// Check start compute status.
        /// </summary>
        CheckStartCompute = 4,

        /// <summary>
        /// Kick off Heartbeat Monitoring.
        /// </summary>
        StartHeartbeatMonitoring = 5,

         /// <summary>
        /// Create Heartbeat record.
        /// </summary>
        GetHeartbeatRecord = 6,
    }
}