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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    /// <summary>
    /// Message pump which gates messages to/from the underlying queue.
    /// </summary>
    public class ContinuationTaskMessagePump : IContinuationTaskMessagePump
    {
        private const string LogBaseName = ResourceLoggingConstants.ContinuationTaskMessagePump;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationTaskMessagePump"/> class.
        /// </summary>
        /// <param name="continuationTaskWorkerPoolManager">Targer pool manager.</param>
        /// <param name="resourceJobQueueRepository">Underlying resourcec job queue repository.</param>
        public ContinuationTaskMessagePump(
            IContinuationTaskWorkerPoolManager continuationTaskWorkerPoolManager,
            IResourceJobQueueRepository resourceJobQueueRepository)
        {
            ContinuationTaskWorkerPoolManager = continuationTaskWorkerPoolManager;
            ResourceJobQueueRepository = resourceJobQueueRepository;
            MessageCache = new ConcurrentQueue<CloudQueueMessage>();
        }

        private IContinuationTaskWorkerPoolManager ContinuationTaskWorkerPoolManager { get; }

        private IResourceJobQueueRepository ResourceJobQueueRepository { get; }

        private ConcurrentQueue<CloudQueueMessage> MessageCache { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunTryPopulateCacheAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_try_populate_cache",
                async () =>
                {
                    var targetMessageCacheLength = ContinuationTaskWorkerPoolManager.CurrentWorkerCount;

                    logger.FluentAddValue("PumpPreCacheLevel", MessageCache.Count)
                        .FluentAddValue("PumpTargetLevel", targetMessageCacheLength);

                    // Only trigger work when we have something to really do
                    if (MessageCache.Count < (targetMessageCacheLength / 2))
                    {
                        logger.FluentAddValue("PumpFillDidTrigger", true.ToString());

                        // Fetch items
                        var items = await ResourceJobQueueRepository.GetAsync(targetMessageCacheLength - MessageCache.Count, logger.WithValues(new LogValueSet()));

                        logger.FluentAddValue("PumpFoundItems", items.Count().ToString());

                        // Add each item to the local cache
                        foreach (var item in items)
                        {
                            MessageCache.Enqueue(item);
                        }
                    }

                    logger.FluentAddValue("PumpPostCacheLevel", MessageCache.Count);

                    return !Disposed;
                });
        }

        /// <inheritdoc/>
        public Task<CloudQueueMessage> GetMessageAsync(IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_get_message",
                async () =>
                {
                    logger.FluentAddValue("PumpPreCacheLevel", MessageCache.Count);

                    // Try and get from cache
                    var cacheHit = MessageCache.TryDequeue(out var message);

                    logger.FluentAddValue("PumpCacheHit", cacheHit);

                    // Try getting from cache and manually pull if needed. Note, doesn't really
                    // matter if a cache miss happens here, its a nice to have not a necessity.
                    if (!cacheHit)
                    {
                        message = await ResourceJobQueueRepository.GetAsync(logger.WithValues(new LogValueSet()));
                    }

                    logger.FluentAddValue("PumpFoundMessage", message != null);
                    if (message != null)
                    {
                        logger.FluentAddValue("PumpDequeueCount", message.DequeueCount)
                            .FluentAddValue("PumpExpirationTime", message.ExpirationTime)
                            .FluentAddValue("PumpInsertionTime", message.InsertionTime)
                            .FluentAddValue("PumpNextVisibleTime", message.NextVisibleTime);
                    }

                    logger.FluentAddValue("PumpPostCacheLevel", MessageCache.Count);

                    return message;
                });
        }

        /// <inheritdoc/>
        public async Task DeleteMessageAsync(CloudQueueMessage message, IDiagnosticsLogger logger)
        {
            await ResourceJobQueueRepository.DeleteAsync(message, logger);
        }

        /// <inheritdoc/>
        public async Task PushMessageAsync(ResourceJobQueuePayload payload, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
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
