// <copyright file="HeartbeatMonitorInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models
{
    /// <summary>
    /// Heartbeat Monitor input.
    /// </summary>
    public class HeartbeatMonitorInput : EnvironmentMonitorContinuationInput
    {
        /// <summary>
        /// Gets or sets the compute resource id.
        /// </summary>
        public Guid ComputeResourceId { get; set; }
    }
}