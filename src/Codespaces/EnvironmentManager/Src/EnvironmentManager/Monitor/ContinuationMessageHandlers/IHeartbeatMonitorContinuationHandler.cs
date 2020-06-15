// <copyright file="IHeartbeatMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.ContinuationMessageHandlers
{
    /// <summary>
    /// Marker interface for the Heartbeat Monitor Handler.
    /// </summary>
    public interface IHeartbeatMonitorContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}