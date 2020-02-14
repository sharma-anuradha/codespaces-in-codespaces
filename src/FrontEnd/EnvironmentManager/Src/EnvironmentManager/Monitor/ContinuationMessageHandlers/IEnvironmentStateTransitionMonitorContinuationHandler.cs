// <copyright file="IEnvironmentStateTransitionMonitorContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager
{
    /// <summary>
    /// Environment State Transition Monitor Continuation Handler.
    /// </summary>
    public interface IEnvironmentStateTransitionMonitorContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}