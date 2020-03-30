// <copyright file="CreateEnvironmentContinuationInputState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Create environment continuation state.
    /// </summary>
    public enum CreateEnvironmentContinuationInputState
    {
        /// <summary>
        /// Allocate resource.
        /// </summary>
        AllocateResource = 0,

        /// <summary>
        /// Check Resource State.
        /// </summary>
        CheckResourceState = 1,

        /// <summary>
        /// Kick off start compute.
        /// </summary>
        StartCompute = 2,

        /// <summary>
        /// Check start compute status.
        /// </summary>
        CheckStartCompute = 3,

        /// <summary>
        /// Kick off Heartbeat Monitoring.
        /// </summary>
        StartHeartbeatMonitoring = 4,
    }
}