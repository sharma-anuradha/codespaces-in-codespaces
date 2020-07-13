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
        public BufferBlockQueue()
        {
            this.invisibleMessagesTask = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000, DisposeToken);
                    var now = DateTime.Now;
                    foreach (var item in InvisibleMessages.Values.Where(i => now > i.Item2).ToArray())
                    {
                        InvisibleMessages.TryRemove(item.Item1.Id, out var removed);
                        await AddMessageAsync(item.Item1, DisposeToken);
                    }
                }
            });
        }

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
                            InvisibleMessages[queueMessage.Id] = new Tuple<QueueMessage, DateTime>(queueMessage, DateTime.Now.Add(visibilityTimeout.Value));
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
                if (updateContent)
                {
                    invisibleQueue.Item1.Content = queueMessage.Content;
                }

                InvisibleMessages[queueMessage.Id] = new Tuple<QueueMessage, DateTime>(invisibleQueue.Item1, DateTime.Now.Add(visibilityTimeout));
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
            await BufferBlock.Completion;
        }

        private async Task AddMessageAsync(QueueMessage queueMessage, CancellationToken cancellationToken)
        {
            await BufferBlock.SendAsync(queueMessage, cancellationToken);
            QueueMessages[queueMessage.Id] = queueMessage;
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
