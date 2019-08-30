// <copyright file="ContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    ///
    /// </summary>
    public class ContinuationTaskActivator : IContinuationTaskActivator
    {
        private const string LogBaseName = ResourceLoggingsConstants.ContinuationTaskActivator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskActivator"/> class.
        /// </summary>
        /// <param name="messagePump"></param>
        public ContinuationTaskActivator(
            IEnumerable<IContinuationTaskMessageHandler> handlers,
            IContinuationTaskMessagePump messagePump)
        {
            Handlers = handlers;
            MessagePump = messagePump;
        }

        private IEnumerable<IContinuationTaskMessageHandler> Handlers { get; }

        private IContinuationTaskMessagePump MessagePump { get; }

        /// <inheritdoc/>
        public async Task<ContinuationResult> Execute(string name, object input, IDiagnosticsLogger logger)
        {
            var payload = new ResourceJobQueuePayload
            {
                TrackingId = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Status = null,
                ContinuationToken = null,
                Input = input,
                Metadata = null,
                Target = name,
            };

            return (await Continue(payload, logger))?.Result;
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Continue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            var continueFindDuration = logger
                .FluentAddBaseValue("ContinuationActivatorId", Guid.NewGuid().ToString())
                .FluentAddBaseValue("ContinuationTrackingId", payload.TrackingId)
                .FluentAddValue("ContinuationIsInitial", string.IsNullOrEmpty(payload.ContinuationToken).ToString())
                .FluentAddValue("ContinuationPreStatus", payload.Status.ToString())
                .StartDuration();

            var result = (ContinuationTaskMessageHandlerResult)null;
            var didHandle = false;
            foreach (var handler in Handlers)
            {
                // Check if this handler can handle this message
                if (handler.CanHandle(payload))
                {
                    logger.FluentAddValue("ContinuationHandler", handler.GetType().Name);

                    var continueDuration = logger.StartDuration();

                    // Activate the core continuation
                    result = await Continue(handler, payload, logger);

                    logger.FluentAddValue("ContinuationPostStatus", result.Result.Status.ToString())
                        .FluentAddValue("ContinuationRetryAfter", result.Result.RetryAfter.ToString())
                        .FluentAddValue("ContinuationIsFinal", string.IsNullOrEmpty(result.Result.ContinuationToken).ToString());

                    didHandle = true;
                    break;
                }
            }

            logger.FluentAddValue("ContinuationWasHandled", didHandle.ToString())
                .FluentAddValue("ContinuationFindHandleDuration", continueFindDuration.Elapsed.TotalMilliseconds.ToString())
                .LogInfo($"{LogBaseName}-continue-complete");

            return result;
        }

        private async Task<ContinuationTaskMessageHandlerResult> Continue(IContinuationTaskMessageHandler handler, ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            // Run the core continuation
            var input = new ContinuationTaskMessageHandlerInput
            {
                Input = payload.Input,
                Metadata = payload.Metadata,
                Status = payload.Status,
                ContinuationToken = payload.ContinuationToken,
            };
            var result = await handler.Continue(input, logger.FromExisting());

            // If we have another result pending put onto queue
            if (!string.IsNullOrEmpty(result.Result.ContinuationToken))
            {
                var nextPayload = payload;
                nextPayload.ContinuationToken = result.Result.ContinuationToken;
                nextPayload.Status = result.Result.Status;
                nextPayload.Metadata = result.Metadata;

                await MessagePump.AddPayloadAsync(nextPayload, result.Result.RetryAfter, logger.FromExisting());
            }

            return result;
        }
    }
}
