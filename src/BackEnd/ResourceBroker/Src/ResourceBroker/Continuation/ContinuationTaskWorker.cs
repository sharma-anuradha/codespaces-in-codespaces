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

        /// <inheritdoc/>
        public bool Disposed { get; private set; }

        private IContinuationTaskActivator Activator { get; }

        private IContinuationTaskMessagePump MessagePump { get; }

        private Random Random { get; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) =>
                {

                    var rootTimer = Stopwatch.StartNew();
                    childLogger.FluentAddBaseValue("ContinuationWorkerRunId", Guid.NewGuid())
                        .FluentAddValue("ContinuationActivityLevel", ActivityLevel);

                    // Get message from the queue
                    var message = await MessagePump.GetMessageAsync(childLogger.NewChildLogger());

                    childLogger.FluentAddValue("WorkerFoundMessages", message != null);

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

                        childLogger.FluentAddValue("MessageDequeueCount", message.DequeueCount)
                            .FluentAddValue("MessageExpirationTime", message.ExpirationTime)
                            .FluentAddValue("MessageInsertionTime", message.InsertionTime)
                            .FluentAddValue("MessageNextVisibleTime", message.NextVisibleTime)
                            .FluentAddBaseValue("ContinuationPayloadTrackingId", payload.TrackingId)
                            .FluentAddValue("ContinuationPayloadHandleTarget", payload.Target)
                            .FluentAddValue("ContinuationPayloadIsInitial", !payload.Status.HasValue)
                            .FluentAddValue("ContinuationPayloadPreRetryAfter", payload.RetryAfter)
                            .FluentAddValue("ContinuationPayloadPreStatus", payload.Status)
                            .FluentAddValue("ContinuationPayloadCreated", payload.Created)
                            .FluentAddValue("ContinuationPayloadCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds)
                            .FluentAddValue("ContinuationPayloadStepCount", payload.StepCount)
                            .FluentAddValues(payload.LoggerProperties);

                        // Try and handle message
                        var resultPayload = await childLogger.TrackDurationAsync(
                            "WorkerActivator", () => Activator.Continue(payload, childLogger.NewChildLogger()));

                        childLogger.FluentAddValue("ContinuationWasHandled", resultPayload != null);

                        // Deals with error case
                        if (resultPayload != null)
                        {
                            childLogger.FluentAddValue("ContinuationPayloadPostStatus", resultPayload.Status)
                                .FluentAddValue("ContinuationPayloadPostRetryAfter", resultPayload.RetryAfter)
                                .FluentAddValue("ContinuationPayloadIsFinal", resultPayload.Input == null);
                        }

                        // Delete message when we are done
                        await MessagePump.DeleteMessageAsync(message, childLogger.NewChildLogger());

                        childLogger.FluentAddDuration("WorkerRun", rootTimer);
                    }
                    else
                    {
                        childLogger.FluentAddDuration("WorkerRun", rootTimer);

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
                },
                (e, childLogger) => !Disposed,
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        private ResourceJobQueuePayload GetTypedPayload(CloudQueueMessage message)
        {
            return JsonConvert.DeserializeObject<ResourceJobQueuePayload>(
                message.AsString, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        }
    }
}
