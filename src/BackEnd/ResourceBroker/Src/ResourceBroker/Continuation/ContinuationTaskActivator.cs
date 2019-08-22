// <copyright file="ContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskActivator : IContinuationTaskActivator
    {
        public ContinuationTaskActivator(
            IContinuationTaskMessagePump messagePump)
        {
            MessagePump = messagePump;
        }

        private IContinuationTaskMessagePump MessagePump { get; }

        /// <inheritdoc/>
        public Task<ContinuationTaskMessageHandlerResult> Execute(IContinuationTaskMessageHandler handler, object input, string name, IDiagnosticsLogger logger)
        {
            var payload = new ResourceJobQueuePayload
            {
                TrackingId = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Status = "Initialized",
                ContinuationToken = null,
                Input = input,
                Metadata = null,
                Target = name,
            };

            return Execute(handler, payload, logger);
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Execute(IContinuationTaskMessageHandler handler, ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            // Run the core continuation
            var inpuut = new ContinuationTaskMessageHandlerInput
            {
                HandlerInput = payload.Input,
                Metadata = payload.Metadata,
            };
            var result = await handler.Continue(inpuut, logger, payload.ContinuationToken);

            // If we have another result pending put onto queue
            if (!string.IsNullOrEmpty(result.HandlerResult.ContinuationToken))
            {
                var nextPayload = payload;
                nextPayload.ContinuationToken = result.HandlerResult.ContinuationToken;
                nextPayload.Status = result.HandlerResult.Status;
                nextPayload.Metadata = result.Metadata;

                await MessagePump.AddPayloadAsync(nextPayload, result.HandlerResult.RetryAfter, logger);
            }

            return result;
        }
    }
}
