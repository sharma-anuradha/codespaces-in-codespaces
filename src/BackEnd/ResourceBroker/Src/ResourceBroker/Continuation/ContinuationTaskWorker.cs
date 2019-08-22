// <copyright file="ContinuationTaskWorker.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskWorker : IContinuationTaskWorker
    {
        private static readonly TimeSpan MissDelayTime = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskWorker"/> class.
        /// </summary>
        /// <param name="activator"></param>
        /// <param name="messagePump"></param>
        /// <param name="handlers"></param>
        public ContinuationTaskWorker(
            IContinuationTaskActivator activator,
            IContinuationTaskMessagePump messagePump,
            IEnumerable<IContinuationTaskMessageHandler> handlers)
        {
            Activator = activator;
            MessagePump = messagePump;
            Handlers = handlers;
            ActivityLevel = 100;
        }

        /// <inheritdoc/>
        public int ActivityLevel { get; private set; }

        private IContinuationTaskActivator Activator { get; }

        private IContinuationTaskMessagePump MessagePump { get; }

        private IEnumerable<IContinuationTaskMessageHandler> Handlers { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public async Task<bool> Run(IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("ContinuationRunId", Guid.NewGuid().ToString());
            var rootLogger = logger.FromExisting();
            logger.FluentAddValue("ContinuationActivityLevel", ActivityLevel.ToString());

            // Get message from the queue
            var message = await MessagePump.GetMessageAsync(rootLogger);

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
                var typedPayload = message.GetTypedPayload<ResourceJobQueuePayload>();

                logger
                    .FluentAddValue("ContinuationTrackingId", typedPayload.TrackingId)
                    .FluentAddValue("ContinuationTarget", typedPayload.Target)
                    .FluentAddValue("ContinuationCreateOffSet", (DateTime.UtcNow - typedPayload.Created).TotalMilliseconds.ToString());

                // Try and handle message
                var didHandle = false;
                foreach (var handler in Handlers)
                {
                    // Check if this handler can handle this message
                    if (handler.CanHandle(typedPayload))
                    {
                        logger.FluentAddValue("ContinuationHandler", handler.GetType().Name);

                        var continueDuration = logger.StartDuration();

                        // Activate the core continuation
                        var result = await Activator.Execute(handler, typedPayload, rootLogger);

                        logger
                            .FluentAddValue("ContinuationHandleDuration", continueDuration.Elapsed.TotalMilliseconds.ToString())
                            .FluentAddValue("ContinuationHasNext", (!string.IsNullOrEmpty(result.HandlerResult.ContinuationToken)).ToString());

                        didHandle = true;
                        break;
                    }
                }

                logger.FluentAddValue("ContinuationWasHandled", didHandle.ToString());

                // Delete message when we are done
                await MessagePump.DeleteMessage(message, rootLogger);
            }
            else
            {
                // Tracking activity level, currently very basic
                if (ActivityLevel > 0)
                {
                    ActivityLevel--;
                }

                await Task.Delay(MissDelayTime);
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
