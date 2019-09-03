// <copyright file="ContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Newtonsoft.Json;

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
            var rootTimer = Stopwatch.StartNew();
            logger.FluentAddBaseValue("ContinuationWorkerRunId", Guid.NewGuid())
                .FluentAddValue("ContinuationActivityLevel", ActivityLevel);

            // Get message from the queue
            var message = await MessagePump.GetMessageAsync(logger.WithValues(new LogValueSet()));

            logger.FluentAddValue("WorkerFoundMessages", message != null);

            // Process messages if we can
            if (message != null)
            {
                // Tracking activity level, currently very basic
                if (ActivityLevel < 200)
                {
                    ActivityLevel++;
                }

                // Pull out typed message content
                var payload = GetTypedPayload(message);

                logger.FluentAddBaseValue("ContinuationPayloadTrackingId", payload.TrackingId)
                    .FluentAddValue("ContinuationPayloadHandleTarget", payload.Target)
                    .FluentAddValue("ContinuationPayloadIsInitial", !payload.Status.HasValue)
                    .FluentAddValue("ContinuationPayloadPreStatus", payload.Status)
                    .FluentAddValue("ContinuationPayloadCreated", payload.Created)
                    .FluentAddValue("ContinuationPayloadCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds);

                // Try and handle message
                var resultPayload = await logger.TrackDurationAsync(
                    "WorkerActivator", () => Activator.Continue(payload, logger.WithValues(new LogValueSet())));

                logger.FluentAddValue("ContinuationWasHandled", resultPayload != null);

                // Deals with error case
                if (resultPayload != null)
                {
                    logger.FluentAddValue("ContinuationPayloadPostStatus", resultPayload.Status)
                        .FluentAddValue("ContinuationPayloadPostRetryAfter", resultPayload.RetryAfter)
                        .FluentAddValue("ContinuationPayloadIsFinal", resultPayload.Input == null);
                }

                // Delete message when we are done
                await MessagePump.DeleteMessageAsync(message, logger.WithValues(new LogValueSet()));

                logger.FluentAddDuration("WorkerRun", rootTimer);
            }
            else
            {
                logger.FluentAddDuration("WorkerRun", rootTimer);

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

        private ResourceJobQueuePayload GetTypedPayload(CloudQueueMessage message)
        {
            return JsonConvert.DeserializeObject<ResourceJobQueuePayload>(
                message.AsString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
