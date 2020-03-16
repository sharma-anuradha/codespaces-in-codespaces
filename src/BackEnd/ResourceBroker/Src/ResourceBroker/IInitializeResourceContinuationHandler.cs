// <copyright file="IInitializeResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker
{
    /// <summary>
    /// Marker interface for the Initialize Resource Handler.
    /// </summary>
    public interface IInitializeResourceContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
