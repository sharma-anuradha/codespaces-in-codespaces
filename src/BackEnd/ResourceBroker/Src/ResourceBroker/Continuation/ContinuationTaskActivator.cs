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
        public Task<ContinuationResult> Execute(string name, ContinuationInput input, IDiagnosticsLogger logger, Guid? trackingId = null)
        {
            trackingId = trackingId ?? Guid.NewGuid();

            var payload = new ResourceJobQueuePayload
            {
                TrackingId = trackingId.ToString(),
                Created = DateTime.UtcNow,
                Status = null,
                Input = input,
                Target = name,
            };

            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) => (await InnerContinue(payload, childLogger)).Result);
        }

        /// <inheritdoc/>
        public Task<ResourceJobQueuePayload> Continue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) => (await InnerContinue(payload, childLogger)).ResultPayload);
        }

        private async Task<(ResourceJobQueuePayload ResultPayload, ContinuationResult Result)> InnerContinue(ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ContinuationActivatorId", Guid.NewGuid())
                .FluentAddBaseValue("ContinuationPayloadTrackingId", payload.TrackingId)
                .FluentAddValue("ContinuationPayloadHandleTarget", payload.Target)
                .FluentAddValue("ContinuationPayloadIsInitial", !payload.Status.HasValue)
                .FluentAddValue("ContinuationPayloadPreStatus", payload.Status)
                .FluentAddValue("ContinuationPayloadCreated", payload.Created)
                .FluentAddValue("ContinuationPayloadCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds)
                .FluentAddValue("ContinuationPayloadStepCount", payload.StepCount);

            var continueFindDuration = logger.StartDuration();

            var result = (ContinuationResult)null;
            var resultPayload = (ResourceJobQueuePayload)null;

            var didHandle = false;
            foreach (var handler in Handlers)
            {
                // Check if this handler can handle this message
                if (handler.CanHandle(payload))
                {
                    logger.FluentAddValue("ContinuationHandler", handler.GetType().Name);

                    // Activate the core continuation
                    (resultPayload, result) = await InnerContinue(handler, payload, logger);

                    // Make sure that we have a result to work for
                    if (result != null)
                    {
                        logger.FluentAddValue("ContinuationPayloadPostStatus", result.Status)
                            .FluentAddValue("ContinuationPayloadPostErrorReason", result.ErrorReason)
                            .FluentAddValue("ContinuationPayloadPostRetryAfter", result.RetryAfter)
                            .FluentAddValue("ContinuationPayloadIsFinal", resultPayload.Input == null)
                            .FluentAddValue("ContinuationHandlerToken", result.NextInput?.ContinuationToken);
                    }

                    didHandle = true;
                    break;
                }
            }

            logger.FluentAddValue("ContinuationWasHandled", didHandle)
                .FluentAddValue("ContinuationFindHandleDuration", continueFindDuration.Elapsed.TotalMilliseconds);

            return (resultPayload, result);
        }

        private async Task<(ResourceJobQueuePayload ResultPayload, ContinuationResult Result)> InnerContinue(IContinuationTaskMessageHandler handler, ResourceJobQueuePayload payload, IDiagnosticsLogger logger)
        {
            // Result is based off the current
            var resultPayload = new ResourceJobQueuePayload(payload.TrackingId, payload.Target, payload.Created, payload.StepCount + 1);

            // Run the continuation
            var result = (ContinuationResult)null;
            try
            {
                result = await logger.TrackDurationAsync(
                    "ContinuationHandler", () => handler.Continue(payload.Input, logger.WithValues(new LogValueSet())));
            }
            catch (ContinuationTaskTemporarilyUnavailableException e)
            {
                // Swallowing the exception
                logger.FluentAddValue("ContinuationHandlerTemporarilyUnavailable", true)
                    .FluentAddValue("ContinuationHandlerTemporarilyUnavailableMessage", e.Message);

                result = new ContinuationResult
                    {
                        NextInput = payload.Input,
                        RetryAfter = e.RetryAfter,
                        Status = payload.Status.GetValueOrDefault(),
                    };
            }
            catch (Exception e)
            {
                // Swallowing the exception
                logger.FluentAddValue("ContinuationHandlerExceptionThrew", true)
                    .FluentAddValue("ContinuationHandlerExceptionMessage", e.Message);
            }

            logger.FluentAddValue("ContinuationHandlerFailed", result == null);

            // Make sure that the handler didn't fail
            if (result != null)
            {
                // Setup next payload
                resultPayload.Status = result.Status;
                resultPayload.Input = result.NextInput;
                resultPayload.RetryAfter = result.RetryAfter;

                // Put onto quueue if not finished
                if (!result.Status.IsFinal())
                {
                    await MessagePump.PushMessageAsync(resultPayload, result.RetryAfter, logger.WithValues(new LogValueSet()));
                }
            }
            else
            {
                // If we don't have a result we are in an error state
                resultPayload.Status = OperationState.Failed;
            }

            return (resultPayload, result);
        }
    }
}
