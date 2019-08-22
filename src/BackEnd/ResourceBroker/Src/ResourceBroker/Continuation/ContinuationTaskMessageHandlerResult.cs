// <copyright file="ContinuationTaskMessageHandlerResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskMessageHandlerResult
    {
        public BaseContinuationResult HandlerResult { get; set; }

        public object Metadata { get; set; }
    }
}
