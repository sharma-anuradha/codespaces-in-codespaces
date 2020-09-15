// <copyright file="BufferBlockQueue.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IQueue interface using a TPL buffer block.
    /// </summary>
    public class BufferBlockQueue : DisposableBase, IQueue
    {
        private readonly Task invisibleMessagesTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferBlockQueue"/> class.
        /// </summary>
        /// <param name="queueId">Queue id.</param>
        public BufferBlockQueue(string queueId)
        {
            Requires.NotNullOrEmpty(queueId, nameof(queueId));
            Id = queueId;

            this.invisibleMessagesTask = Task.Run(async () =>
            {
                while (!DisposeToken.IsCancellationRequested)
                {
                    await Task.Delay(500, DisposeToken);
                    var now = DateTime.Now;
                    var messagesToAdd = InvisibleMessages.Values.Where(i => now > i.Item2).ToArray();
                    foreach (var item in messagesToAdd)
                    {
                        InvisibleMessages.TryRemove(item.Item1.Id, out var removed);
                        await AddMessageAsync(item.Item1, DisposeToken);
                    }
                }
            });
        }

        /// <inheritdoc/>
        public string Id { get; }

        private BufferBlock<QueueMessage> BufferBlock { get; } = new BufferBlock<QueueMessage>();

        private ConcurrentDictionary<string, QueueMessage> QueueMessages { get; } = new ConcurrentDictionary<string, QueueMessage>();

        private ConcurrentDictionary<string, Tuple<QueueMessage, DateTime>> InvisibleMessages { get; } = new ConcurrentDictionary<string, Tuple<QueueMessage, DateTime>>();

        /// <inheritdoc/>
        public async Task<QueueMessage> AddMessageAsync(byte[] content, TimeSpan? initialVisibilityDelay, CancellationToken cancellationToken)
        {
            var queueMessage = new QueueMessageAdapter(Guid.NewGuid().ToString(), content);
            if (initialVisibilityDelay.HasValue)
            {
                var delayTask = Task.Run(async () =>
                {
                    await Task.Delay(initialVisibilityDelay.Value, DisposeToken);
                    await AddMessageAsync(queueMessage, cancellationToken);
                });
            }
            else
            {
                await AddMessageAsync(queueMessage, cancellationToken);
            }

            return queueMessage;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<QueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var queueMessages = new List<QueueMessage>();

            while (messageCount != 0)
            {
                try
                {
                    var queueMessage = await BufferBlock.ReceiveAsync(timeout, cancellationToken);
                    if (QueueMessages.TryRemove(queueMessage.Id, out var removed))
                    {
                        if (visibilityTimeout.HasValue)
                        {
                            var visibilityExpired = DateTime.Now.Add(visibilityTimeout.Value);
                            InvisibleMessages[queueMessage.Id] = new Tuple<QueueMessage, DateTime>(queueMessage, visibilityExpired);
                        }

                        queueMessages.Add(queueMessage);
                        --messageCount;
                    }
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            return queueMessages;
        }

        /// <inheritdoc/>
        public Task DeleteMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken)
        {
            InvisibleMessages.TryRemove(queueMessage.Id, out var removed);
            QueueMessages.TryRemove(queueMessage.Id, out var queueMessageRemoved);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task UpdateMessageAsync(QueueMessage queueMessage, bool updateContent, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            if (InvisibleMessages.TryGetValue(queueMessage.Id, out var invisibleQueue))
            {
                DateTime visibilityDateTime;
                if (updateContent)
                {
                    invisibleQueue.Item1.Content = queueMessage.Content;
                    visibilityDateTime = invisibleQueue.Item2;
                }
                else
                {
                    visibilityDateTime = DateTime.Now.Add(visibilityTimeout);
                }

                InvisibleMessages[queueMessage.Id] = new Tuple<QueueMessage, DateTime>(invisibleQueue.Item1, visibilityDateTime);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
        {
            try
            {
                await this.invisibleMessagesTask;
            }
            catch (OperationCanceledException)
            {
            }

            BufferBlock.Complete();
            BufferBlock.TryReceiveAll(out var items);
            await BufferBlock.Completion;
        }

        private async Task AddMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken)
        {
            QueueMessages[queueMessage.Id] = queueMessage;
            await BufferBlock.SendAsync(queueMessage, cancellationToken);
        }

        private class QueueMessageAdapter : QueueMessage
        {
            public QueueMessageAdapter(string id, byte[] content)
            {
                Id = id;
                Content = content;
            }

            public override string Id { get; }

            public override byte[] Content { get; set; }
        }
    }
}
