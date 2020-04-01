﻿// <copyright file="ContinuationTaskActivator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Continuation Activator which works with the supplied message and the
    /// available handlers to trigger the targetted handler.
    /// </summary>
    public class ContinuationTaskActivator : IContinuationTaskActivator, ICrossRegionContinuationTaskActivator
    {
        private const string LogBaseName = "continuation_task_activator";

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskActivator"/> class.
        /// </summary>
        /// <param name="handlers">Registered handlers in the system.</param>
        /// <param name="messagePump">Message pump that can be used to put next
        /// messages onto the queue.</param>
        /// <param name="crossRegionMessagePump">Message pump that can be used to put next
        /// messages onto the queue for another control plane region.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="currentLocationProvider">Current location provider.</param>
        public ContinuationTaskActivator(
            IEnumerable<IContinuationTaskMessageHandler> handlers,
            IContinuationTaskMessagePump messagePump,
            ICrossRegionContinuationTaskMessagePump crossRegionMessagePump,
            IControlPlaneInfo controlPlaneInfo,
            ICurrentLocationProvider currentLocationProvider)
        {
            Handlers = handlers;
            MessagePump = messagePump;
            CrossRegionMessagePump = crossRegionMessagePump;
            ControlPlaneInfo = controlPlaneInfo;
            CurrentLocationProvider = currentLocationProvider;
        }

        private IEnumerable<IContinuationTaskMessageHandler> Handlers { get; }

        private IContinuationTaskMessagePump MessagePump { get; }

        private ICrossRegionContinuationTaskMessagePump CrossRegionMessagePump { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICurrentLocationProvider CurrentLocationProvider { get; }

        /// <inheritdoc/>
        public Task<ContinuationResult> Execute(
            string name,
            ContinuationInput input,
            IDiagnosticsLogger logger,
            Guid? trackingId = null,
            IDictionary<string, string> loggerProperties = null)
        {
            var payload = ConstructPayload(name, input, logger, ref trackingId, loggerProperties);

            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) => (await InnerContinue(payload, childLogger)).Result);
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> ExecuteForDataPlane(
            string name,
            AzureLocation dataPlaneRegion,
            ContinuationInput input,
            IDiagnosticsLogger logger,
            Guid? systemId = null,
            IDictionary<string, string> loggerProperties = null)
        {
            var controlPlaneRegion = ControlPlaneInfo.GetOwningControlPlaneStamp(dataPlaneRegion).Location;
            if (CurrentLocationProvider.CurrentLocation == controlPlaneRegion)
            {
                return Execute(name, input, logger, systemId, loggerProperties);
            }

            var payload = ConstructPayload(name, input, logger, ref systemId, loggerProperties);

            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) =>
                {
                    await CrossRegionMessagePump.PushMessageToControlPlaneRegionAsync(payload, controlPlaneRegion, TimeSpan.Zero, logger.WithValues(new LogValueSet()));
                    return new ContinuationResult
                    {
                        Status = OperationState.Triggered,
                    };
                });
        }

        /// <inheritdoc/>
        public Task<ContinuationQueuePayload> Continue(ContinuationQueuePayload payload, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) => (await InnerContinue(payload, childLogger)).ResultPayload);
        }

        private static ContinuationQueuePayload ConstructPayload(string name, ContinuationInput input, IDiagnosticsLogger logger, ref Guid? trackingId, IDictionary<string, string> loggerProperties)
        {
            var trackingInstanceId = Guid.NewGuid();
            trackingId = trackingId ?? trackingInstanceId;

            var payload = new ContinuationQueuePayload
            {
                TrackingId = trackingId.ToString(),
                TrackingInstanceId = trackingInstanceId.ToString(),
                Created = DateTime.UtcNow,
                Status = null,
                Input = input,
                Target = name,
                LoggerProperties = loggerProperties,
            };

            logger.FluentAddBaseValues(payload.LoggerProperties);
            return payload;
        }

        private async Task<(ContinuationQueuePayload ResultPayload, ContinuationResult Result)> InnerContinue(ContinuationQueuePayload payload, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ContinuationActivatorId", Guid.NewGuid())
                .FluentAddBaseValue("ContinuationPayloadTrackingId", payload.TrackingId)
                .FluentAddBaseValue("ContinuationPayloadTrackingInstanceId", payload.TrackingInstanceId)
                .FluentAddValue("ContinuationPayloadHandleTarget", payload.Target)
                .FluentAddValue("ContinuationPayloadIsInitial", !payload.Status.HasValue)
                .FluentAddValue("ContinuationPayloadPreStatus", payload.Status)
                .FluentAddValue("ContinuationPayloadCreated", payload.Created)
                .FluentAddValue("ContinuationPayloadCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds)
                .FluentAddValue("ContinuationPayloadStepCount", payload.StepCount)
                .FluentAddBaseValues(payload.LoggerProperties);

            var continueFindDuration = logger.StartDuration();

            var result = (ContinuationResult)null;
            var resultPayload = (ContinuationQueuePayload)null;

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

        private async Task<(ContinuationQueuePayload ResultPayload, ContinuationResult Result)> InnerContinue(IContinuationTaskMessageHandler handler, ContinuationQueuePayload payload, IDiagnosticsLogger logger)
        {
            // Result is based off the current
            var resultPayload = new ContinuationQueuePayload(
                payload.TrackingId, payload.TrackingInstanceId, payload.Target, payload.Created, payload.StepCount + 1, payload.LoggerProperties);

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

            var isRunningTimeValid = resultPayload.Created > DateTime.UtcNow.AddHours(-1);

            logger.FluentAddValue("ContinuationHandlerFailed", result == null)
                .FluentAddValue("ContinuationIsRunningTimeValid", isRunningTimeValid);

            // Make sure that the handler didn't fail
            if (result != null && isRunningTimeValid)
            {
                // Setup next payload
                resultPayload.Status = result.Status;
                resultPayload.Input = result.NextInput;
                resultPayload.RetryAfter = result.RetryAfter;

                // Put onto queue if not finished
                if (!result.Status.IsFinal())
                {
                    await MessagePump.PushMessageAsync(resultPayload, result.RetryAfter, logger.WithValues(new LogValueSet()));
                }
            }
            else
            {
                // If we don't have a result we are in an error state
                resultPayload.Status = OperationState.Failed;
                result = null;
            }

            return (resultPayload, result);
        }
    }
}
