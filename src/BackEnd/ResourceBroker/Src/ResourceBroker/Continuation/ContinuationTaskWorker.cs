// <copyright file="ContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// Continuation worker that gets available messages and passes them off to the activator
    /// for processing.
    /// </summary>
    public class ContinuationTaskWorker : IContinuationTaskWorker
    {
        private const string LogBaseName = ResourceLoggingConstants.ContinuationTaskWorker;
        private static readonly TimeSpan MissDelayTime = TimeSpan.FromSeconds(1);
        private static readonly int LongMinMissDelayTime = 2;
        private static readonly int LongMaxMissDelayTime = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskWorker"/> class.
        /// </summary>
        /// <param name="activator">Activator that figures out which handler can process the message.</param>
        /// <param name="messagePump">Message pump that supplies the next messages.</param>
        public ContinuationTaskWorker(
            IContinuationTaskActivator activator,
            IContinuationTaskMessagePump messagePump)
        {
            ActivityLevel = 100;
            Activator = activator;
            MessagePump = messagePump;
            Random = new Random();
        }

        /// <inheritdoc/>
        public int ActivityLevel { get; private set; }

        private IContinuationTaskActivator Activator { get; }

        private IContinuationTaskMessagePump MessagePump { get; }

        private Random Random { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(LogBaseName, () => InnerRunAsync(logger), swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private async Task<bool> InnerRunAsync(IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ContinuationWorkerRunId", Guid.NewGuid())
                .FluentAddValue("ContinuationActivityLevel", ActivityLevel);

            // Get message from the queue
            var message = await MessagePump.GetMessageAsync(logger.WithValues(new LogValueSet()));

            logger.FluentAddValue("ContinuationFoundMessages", message != null);

            // Process messages if we can
            if (message != null)
            {
                // Tracking activity level, currently very basic
                if (ActivityLevel < 200)
                {
                    ActivityLevel++;
                }

                // Pull out typed message content
                var payload = message.GetTypedPayload<ResourceJobQueuePayload>();

                logger.FluentAddValue("ContinuationPayloadTrackingId", payload.TrackingId)
                    .FluentAddValue("ContinuationPayloadHandleTarget", payload.Target)
                    .FluentAddValue("ContinuationPayloadIsInitial", !payload.Status.HasValue)
                    .FluentAddValue("ContinuationPayloadPreStatus", payload.Status)
                    .FluentAddValue("ContinuationPayloadCreated", payload.Created)
                    .FluentAddValue("ContinuationPayloadCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds);

                // Try and handle message
                var nextPayload = await Activator.Continue(payload, logger.WithValues(new LogValueSet()));

                logger.FluentAddValue("ContinuationWasHandled", nextPayload != null);

                // Deals with error case
                if (nextPayload != null)
                {
                    logger.FluentAddValue("ContinuationPayloadPostStatus", nextPayload.Status)
                        .FluentAddValue("ContinuationPayloadPostRetryAfter", nextPayload.RetryAfter)
                        .FluentAddValue("ContinuationPayloadIsFinal", nextPayload.Input == null);
                }

                // Delete message when we are done
                await MessagePump.DeleteMessage(message, logger.WithValues(new LogValueSet()));
            }
            else
            {
                // Tracking activity level, currently very basic
                if (ActivityLevel > 0)
                {
                    ActivityLevel--;
                    await Task.Delay(MissDelayTime);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(Random.Next(LongMinMissDelayTime, LongMaxMissDelayTime)));
                }
            }

            return !Disposed;
        }
    }
}
