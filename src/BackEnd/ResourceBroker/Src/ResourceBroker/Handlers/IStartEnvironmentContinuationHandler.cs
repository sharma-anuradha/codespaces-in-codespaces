// <copyright file="IStartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Marker interface for the Start Resource Handler.
    /// </summary>
    public interface IStartEnvironmentContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
