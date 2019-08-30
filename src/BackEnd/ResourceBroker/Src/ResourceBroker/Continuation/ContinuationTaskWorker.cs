// <copyright file="ContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    ///
    /// </summary>
    public class ContinuationTaskWorker : IContinuationTaskWorker
    {
        private const string LogBaseName = ResourceLoggingsConstants.ContinuationTaskWorker;
        private static readonly TimeSpan MissDelayTime = TimeSpan.FromSeconds(1);
        private static readonly int LongMinMissDelayTime = 2;
        private static readonly int LongMaxMissDelayTime = 5;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskWorker"/> class.
        /// </summary>
        /// <param name="activator"></param>
        /// <param name="messagePump"></param>
        /// <param name="handlers"></param>
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
        public async Task<bool> RunAsync(IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ContinuationWorkerRunId", Guid.NewGuid().ToString())
                .FluentAddValue("ContinuationActivityLevel", ActivityLevel.ToString());

            // Get message from the queue
            var message = await MessagePump.GetMessageAsync(logger.FromExisting());

            logger.FluentAddValue("ContinuationFoundMessages", (message != null).ToString());

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

                logger.FluentAddValue("ContinuationTrackingId", payload.TrackingId)
                    .FluentAddValue("ContinuationHandleTarget", payload.Target)
                    .FluentAddValue("ContinuationIsInitial", string.IsNullOrEmpty(payload.ContinuationToken).ToString())
                    .FluentAddValue("ContinuationPreStatus", payload.Status.ToString())
                    .FluentAddValue("ContinuationCreated", payload.Created.ToString())
                    .FluentAddValue("ContinuationCreateOffSet", (DateTime.UtcNow - payload.Created).TotalMilliseconds.ToString());

                // Try and handle message
                var result = await Activator.Continue(payload, logger.FromExisting());

                logger.FluentAddValue("ContinuationWasHandled", (result != null).ToString());
                if (result != null)
                {
                    logger.FluentAddValue("ContinuationPostStatus", result.Result.Status.ToString())
                        .FluentAddValue("ContinuationRetryAfter", result.Result.RetryAfter.ToString())
                        .FluentAddValue("ContinuationIsFinal", string.IsNullOrEmpty(result.Result.ContinuationToken).ToString());
                }

                // Delete message when we are done
                await MessagePump.DeleteMessage(message, logger.FromExisting());
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
