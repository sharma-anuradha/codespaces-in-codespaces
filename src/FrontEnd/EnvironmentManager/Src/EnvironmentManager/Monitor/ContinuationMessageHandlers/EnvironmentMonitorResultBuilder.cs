// <copyright file="EnvironmentMonitorResultBuilder.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Monitor.ContinuationMessageHandlers
{
    /// <summary>
    /// Health monitor result builder.
    /// </summary>
    public class EnvironmentMonitorResultBuilder
    {
        /// <summary>
        /// Create result for unhealthy state to stop monitoring.
        /// </summary>
        /// <param name="state">target state.</param>
        /// <param name="reason">target reason.</param>
        /// <returns>result.</returns>
        public static ContinuationResult CreateFinalResult(OperationState state, string reason)
        {
            return new ContinuationResult
            {
                Status = state,
                RetryAfter = TimeSpan.Zero,
                NextInput = default,
                ErrorReason = reason,
            };
        }

        /// <summary>
        /// Create result for healthy state to continue monitoring.
        /// </summary>
        /// <param name="typedInput">target input.</param>
        /// <param name="lastHeartbeatTime">last heartbeat time.</param>
        /// <returns>result.</returns>
        public static ContinuationResult CreateHeartbeatContinuationResult(HeartbeatMonitorInput typedInput, DateTime lastHeartbeatTime)
        {
            return new ContinuationResult
            {
                Status = OperationState.InProgress,
                RetryAfter = DateTime.UtcNow.AddMinutes(EnvironmentMonitorConstants.HeartbeatTimeoutInMinutes + EnvironmentMonitorConstants.BufferInMinutes) - lastHeartbeatTime,
                NextInput = typedInput,
            };
        }
    }
}
