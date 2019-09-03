// <copyright file="ICreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Marker interface for the Create Resource Handler.
    /// </summary>
    public interface ICreateResourceContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
