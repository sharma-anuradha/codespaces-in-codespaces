// <copyright file="ContinuationTaskMessagePump.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation
{
    public class ContinuationTaskMessagePump : IContinuationTaskMessagePump
    {
        private const int TargetMessageCacheLength = 10;

        public ContinuationTaskMessagePump(
            IResourceJobQueueRepository resourceJobQueueRepository)
        {
            ResourceJobQueueRepository = resourceJobQueueRepository;
            MessageCache = new ConcurrentQueue<IResourceJobQueueMessage>();
        }

        private IResourceJobQueueRepository ResourceJobQueueRepository { get; }

        private ConcurrentQueue<IResourceJobQueueMessage> MessageCache { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public async Task<bool> StartAsync(IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("CacheLevel", MessageCache.Count.ToString());

            // Only trigger work when we have something to really do
            if (MessageCache.Count < (TargetMessageCacheLength / 2))
            {
                logger.FluentAddValue("DidExecute", true.ToString());

                // Fetch items
                var items = await ResourceJobQueueRepository.GetAsync(TargetMessageCacheLength - MessageCache.Count, logger);

                logger.FluentAddValue("FoundItems", items.Count().ToString());

                // Add each item to the local cache
                foreach (var item in items)
                {
                    MessageCache.Enqueue(item);
                }
            }

            return !Disposed;
        }

        /// <inheritdoc/>
        public async Task<IResourceJobQueueMessage> GetMessageAsync(IDiagnosticsLogger logger)
        {
            // Try getting from cache and manually pull if needed. Note, doesn't really
            // matter if a cache miss happens here, its a nice to have not a necessity.
            if (!MessageCache.TryDequeue(out var message))
            {
                message = await ResourceJobQueueRepository.GetAsync(logger);
            }

            return message;
        }

        /// <inheritdoc/>
        public async Task DeleteMessage(IResourceJobQueueMessage message, IDiagnosticsLogger logger)
        {
            await ResourceJobQueueRepository.DeleteAsync(message, logger);
        }

        /// <inheritdoc/>
        public async Task AddPayloadAsync(ResourceJobQueuePayload payload, TimeSpan? initialVisibilityDelay, IDiagnosticsLogger logger)
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
