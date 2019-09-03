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
    /// Continuation Activator which works with the supplied message and the
    /// available handlers to trigger the targetted handler.
    /// </summary>
    public class ContinuationTaskActivator : IContinuationTaskActivator
    {
        private const string LogBaseName = ResourceLoggingConstants.ContinuationTaskActivator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskActivator"/> class.
        /// </summary>
        /// <param name="handlers">Registered handlers in the system.</param>
        /// <param name="messagePump">Message pump that can be used to put next 
        /// messages onto the queue.</param>
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
        public Task<ContinuationResult> Execute(string name, ContinuationInput input, IDiagnosticsLogger logger)
        {
            var payload = new ResourceJobQueuePayload
            {
                TrackingId = Guid.NewGuid().ToString(),
                Created = DateTime.UtcNow,
                Status = null,
                Input = input,
                Target = name,
            };

            return logger.OperationScopeAsync(LogBaseName, async () => (await InnerContinue(payload, logger)).Result);
        }

        /// <inheritdoc/>
        public Task<ResourceJobQueuePayload> Continue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(LogBaseName, async () => (await InnerContinue(payload, logger)).NextPayload);
        }

        private async Task<(ResourceJobQueuePayload NextPayload, ContinuationResult Result)> InnerContinue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ContinuationActivatorId", Guid.NewGuid().ToString())
                .FluentAddBaseValue("ContinuationPayloadTrackingId", payload.TrackingId)
                .FluentAddValue("ContinuationPayloadIsInitial", payload.Status.HasValue.ToString())
                .FluentAddValue("ContinuationPayloadPreStatus", payload.Status.ToString());
            var continueFindDuration = logger.StartDuration();

            var result = (ContinuationResult)null;
            var nextPayload = (ResourceJobQueuePayload)null;

            var didHandle = false;
            foreach (var handler in Handlers)
            {
                // Check if this handler can handle this message
                if (handler.CanHandle(payload))
                {
                    logger.FluentAddValue("ContinuationHandler", handler.GetType().Name);

                    // Activate the core continuation
                    (nextPayload, result) = await InnerContinue(handler, payload, logger);

                    // Make sure that we have a result to work for
                    if (result != null)
                    {
                        logger.FluentAddValue("ContinuationPayloadPostStatus", result.Status)
                            .FluentAddValue("ContinuationPayloadIsFinal", result.Status.IsFinal());
                    }

                    didHandle = true;
                    break;
                }
            }

            logger.FluentAddValue("ContinuationWasHandled", didHandle.ToString())
                .FluentAddValue("ContinuationFindHandleDuration", continueFindDuration.Elapsed.TotalMilliseconds.ToString())
                .LogInfo($"{LogBaseName}_continue_complete");

            return (nextPayload, result);
        }

        private async Task<(ResourceJobQueuePayload NextPayload, ContinuationResult Result)> InnerContinue(IContinuationTaskMessageHandler handler, ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            // Run the continuation
            var timer = logger.TrackDuration("ContinuationHandlerDuration");
            var result = await handler.Continue(payload.Input, logger.WithValues(new LogValueSet()));
            timer.Dispose();

            logger.FluentAddValue("ContinuationHandlerFailed", result == null);

            // Make sure that the handler didn't fail
            if (result != null)
            {
                logger.FluentAddValue("ContinuationRetryAfter", result.RetryAfter.ToString());

                // Put onto quueue if not finished
                if (!result.Status.IsFinal())
                {
                    // Setup next payload
                    payload.Input = result.NextInput;
                    payload.Status = result.Status;
                    payload.RetryAfter = result.RetryAfter;

                    await MessagePump.AddPayloadAsync(payload, result.RetryAfter, logger.WithValues(new LogValueSet()));
                }
            }

            return (payload, result);
        }
    }
}
