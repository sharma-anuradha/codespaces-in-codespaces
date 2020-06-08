// <copyright file="ICleanupResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Marker interface for the Delete Cleanup Resource Handler.
    /// </summary>
    public interface ICleanupResourceContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}