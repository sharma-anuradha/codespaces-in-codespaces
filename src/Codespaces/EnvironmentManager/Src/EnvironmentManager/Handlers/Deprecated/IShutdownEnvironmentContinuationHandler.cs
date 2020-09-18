// <copyright file="IShutdownEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Interface for shutdown environment continuation handler.
    /// </summary>
    public interface IShutdownEnvironmentContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
