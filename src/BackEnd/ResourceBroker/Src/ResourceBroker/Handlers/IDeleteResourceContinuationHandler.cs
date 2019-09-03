// <copyright file="IDeleteResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Marker interface for the Delete Resource Handler.
    /// </summary>
    public interface IDeleteResourceContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
