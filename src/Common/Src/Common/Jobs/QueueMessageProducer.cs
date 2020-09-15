// <copyright file="QueueMessageProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IQueueMessageProducer interface.
    /// </summary>
    public class QueueMessageProducer : DisposableBase, IQueueMessageProducer
    {
        private BufferBlock<(QueueMessage, TimeSpan)> bufferBlock;
        private Task getMessagesTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducer"/> class.
        /// </summary>
        /// <param name="queue">A queue instance.</param>
        public QueueMessageProducer(IQueue queue)
        {
            Queue = Requires.NotNull(queue, nameof(queue));
        }

        /// <inheritdoc/>
        public IQueue Queue { get; }

        /// <inheritdoc/>
        public ISourceBlock<(QueueMessage, TimeSpan)> Messages
        {
            get
            {
                if (this.bufferBlock == null)
                {
                    throw new InvalidOperationException("Queue message producer not started");
                }

                return this.bufferBlock;
            }
        }

        /// <inheritdoc/>
        public Task StartAsync(QueueMessageProducerSettings queueMessageProducerSettings, CancellationToken cancellationToken)
        {
            Requires.NotNull(queueMessageProducerSettings, nameof(queueMessageProducerSettings));

            if (this.getMessagesTask != null)
            {
                throw new InvalidOperationException("Already started");
            }

            this.getMessagesTask = StartInternalAsync(queueMessageProducerSettings, cancellationToken);
            return this.getMessagesTask;
        }

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
        {
            if (this.getMessagesTask != null)
            {
                try
                {
                    await this.getMessagesTask;
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task StartInternalAsync(QueueMessageProducerSettings settings, CancellationToken cancellationToken)
        {
            this.bufferBlock = settings.MessageOptions != null ? new BufferBlock<(QueueMessage, TimeSpan)>(settings.MessageOptions) : new BufferBlock<(QueueMessage, TimeSpan)>();
            var boundedCapacity = settings.MessageOptions?.BoundedCapacity;

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposeToken))
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var messageCount = boundedCapacity.HasValue ? boundedCapacity.Value - this.bufferBlock.Count : settings.MessageCount;
                    if (messageCount == 0)
                    {
                        // Note: if we don't have any available slot to buffer a message is better to not even dequeue
                        // any additional message from the cloud
                        await Task.Delay(settings.Timeout, cancellationToken);
                        continue;
                    }

                    var queueMessages = await Queue.GetMessagesAsync(messageCount, settings.VisibilityTimeout, settings.Timeout, cts.Token);
                    if (queueMessages.Any())
                    {
                        foreach (var queueMessage in queueMessages)
                        {
                            await this.bufferBlock.SendAsync((queueMessage, settings.VisibilityTimeout), cts.Token);
                        }
                    }
                }
            }
        }
    }
}
