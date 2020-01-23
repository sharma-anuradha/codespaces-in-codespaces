// <copyright file="ContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation
{
    /// <summary>
    /// Message pump which gates messages to/from the underlying queue.
    /// </summary>
    public class ContinuationTaskMessagePump : IContinuationTaskMessagePump
    {
        private const string LogBaseName = "continuation_task_message_pump";
        private static TimeSpan defaultTimeout = TimeSpan.FromMinutes(2.5);

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskMessagePump"/> class.
        /// </summary>
        /// <param name="continuationTaskWorkerPoolManager">Targer pool manager.</param>
        /// <param name="resourceJobQueueRepository">Underlying resourcec job queue repository.</param>
        public ContinuationTaskMessagePump(
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            IContinuationJobQueueRepository resourceJobQueueRepository)
        {
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            ResourceJobQueueRepository = resourceJobQueueRepository;
            MessageCache = new ConcurrentQueue<CloudQueueMessage>();
        }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private IContinuationJobQueueRepository ResourceJobQueueRepository { get; }

        private ConcurrentQueue<CloudQueueMessage> MessageCache { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunTryPopulateCacheAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_try_populate_cache",
                async (childLogger) =>
                {
                    var targetMessageCacheLength = ContinuationTaskWorkerPoolManager.CurrentWorkerCount;

                    childLogger.FluentAddValue("PumpPreCacheLevel", MessageCache.Count)
                        .FluentAddValue("PumpTargetLevel", targetMessageCacheLength);

                    // Only trigger work when we have something to really do
                    if (MessageCache.Count < targetMessageCacheLength / 2)
                    {
                        childLogger.FluentAddValue("PumpFillDidTrigger", true.ToString());

                        // Fetch items
                        var items = await ResourceJobQueueRepository.GetAsync(
                            targetMessageCacheLength - MessageCache.Count,
                            childLogger.WithValues(new LogValueSet()),
                            defaultTimeout);

                        childLogger.FluentAddValue("PumpFoundItems", items.Count().ToString());

                        // Add each item to the local cache
                        foreach (var item in items)
                        {
                            MessageCache.Enqueue(item);
                        }
                    }

                    childLogger.FluentAddValue("PumpPostCacheLevel", MessageCache.Count);

                    return !Disposed;
                });
        }

        /// <inheritdoc/>
        public Task<CloudQueueMessage> GetMessageAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_message",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("PumpPreCacheLevel", MessageCache.Count);

                    // Try and get from cache
                    var cacheHit = MessageCache.TryDequeue(out var message);

                    childLogger.FluentAddValue("PumpCacheHit", cacheHit);

                    // Try getting from cache and manually pull if needed. Note, doesn't really
                    // matter if a cache miss happens here, its a nice to have not a necessity.
                    if (!cacheHit)
                    {
                        message = await ResourceJobQueueRepository.GetAsync(
                            childLogger.WithValues(new LogValueSet()),
                            defaultTimeout);
                    }

                    childLogger.FluentAddValue("PumpFoundMessage", message != null);
                    if (message != null)
                    {
                        childLogger.FluentAddValue("PumpDequeueCount", message.DequeueCount)
                            .FluentAddValue("PumpExpirationTime", message.ExpirationTime)
                            .FluentAddValue("PumpInsertionTime", message.InsertionTime)
                            .FluentAddValue("PumpNextVisibleTime", message.NextVisibleTime);
                    }

                    childLogger.FluentAddValue("PumpPostCacheLevel", MessageCache.Count);

                    return message;
                });
        }

        /// <inheritdoc/>
        public async Task DeleteMessageAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            await ResourceJobQueueRepository.DeleteAsync(message, logger);
        }

        /// <inheritdoc/>
        public async Task PushMessageAsync(ContinuationQueuePayload payload, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
        {
            await ResourceJobQueueRepository.AddAsync(payload.ToJson(), initialVisibilityDelay, logger);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
