// <copyright file="ICreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Marker interface for the Create Resource Handler.
    /// </summary>
    public interface ICreateResourceContinuationHandler : IContinuationTaskMessageHandler
    {
    }
}
