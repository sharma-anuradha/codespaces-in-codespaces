// <copyright file="ShutdownEnvironmentContinuationInputState.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models
{
    /// <summary>
    /// Shutdown environment continuation input state.
    /// </summary>
    public enum ShutdownEnvironmentContinuationInputState
    {
        /// <summary>
        /// Check on the status of resource.
        /// </summary>
        CheckComputeCleanupStatus,

        /// <summary>
        /// Check on the status of resource.
        /// </summary>
        ComputeDelete,

        /// <summary>
        /// Check on the delete status of the compute.
        /// </summary>
        CheckComputeDeleteStatus,

        /// <summary>
        /// Mark environment shutdown.
        /// </summary>
        MarkShutdown,
    }
}
