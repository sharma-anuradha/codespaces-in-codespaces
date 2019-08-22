// <copyright file="ContinuationTaskMessageHandlerInput.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskMessageHandlerInput
    {
        public object HandlerInput { get; set; }

        public object Metadata { get; set; }
    }
}
