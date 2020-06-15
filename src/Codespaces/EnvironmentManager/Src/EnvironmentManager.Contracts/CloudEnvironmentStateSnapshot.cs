// <copyright file="CloudEnvironmentStateSnapshot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Snapshot of a cloud environment state.
    /// </summary>
    public class CloudEnvironmentStateSnapshot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentStateSnapshot"/> class.
        /// </summary>
        /// <param name="cloudEnvironment">The cloud environment (prior to new state change).</param>
        public CloudEnvironmentStateSnapshot(CloudEnvironment cloudEnvironment)
            : this(cloudEnvironment.State, cloudEnvironment.LastStateUpdated)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEnvironmentStateSnapshot"/> class.
        /// </summary>
        /// <param name="state">The curren stated.</param>
        /// <param name="lastStateUpdated">The last state updated time.</param>
        public CloudEnvironmentStateSnapshot(CloudEnvironmentState state, DateTime lastStateUpdated)
        {
            State = state;
            LastStateUpdated = lastStateUpdated;
        }

        /// <summary>
        /// Gets the cloud environment state.
        /// </summary>
        public CloudEnvironmentState State { get; }

        /// <summary>
        /// Gets the time when the state was last updated.
        /// </summary>
        public DateTime LastStateUpdated { get; }
    }
}
