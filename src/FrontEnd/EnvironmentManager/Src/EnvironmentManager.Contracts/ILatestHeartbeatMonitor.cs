// <copyright file="ILatestHeartbeatMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Keeps track of latest heartbeat timestamp.
    /// </summary>
    public interface ILatestHeartbeatMonitor
    {
        /// <summary>
        /// Gets the timestamp of last heartbeat received.
        /// </summary>
        DateTime? LastHeartbeatReceived { get; }

        /// <summary>
        /// Update last heartbeat received.
        /// </summary>
        /// <param name="heartbeatTimestamp">target heartbeat timestamp.</param>
        void UpdateHeartbeat(DateTime heartbeatTimestamp);
    }
}
