// <copyright file="LatestHeartbeatMonitor.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts;

/// <summary>
/// Keeps track of the latest heartbeat timestamp.
/// </summary>
public class LatestHeartbeatMonitor : ILatestHeartbeatMonitor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LatestHeartbeatMonitor"/> class.
    /// </summary>
    public LatestHeartbeatMonitor()
    {
        LastHeartbeatReceived = null;
    }

    /// <inheritdoc/>
    public DateTime? LastHeartbeatReceived { get; private set; }

    /// <inheritdoc/>
    public void UpdateHeartbeat(DateTime heartbeatTimestamp)
    {
        if (LastHeartbeatReceived == null || heartbeatTimestamp > LastHeartbeatReceived)
        {
            LastHeartbeatReceived = heartbeatTimestamp;
        }
    }
}
