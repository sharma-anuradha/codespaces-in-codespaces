// <copyright file="IContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public interface IContinuationTaskMessageHandler
    {
        bool CanHandle(ResourceJobQueuePayload payload);

        Task<ContinuationTaskMessageHandlerResult> Continue(ContinuationTaskMessageHandlerInput input, IDiagnosticsLogger logger, string continuationToken);
    }
}
