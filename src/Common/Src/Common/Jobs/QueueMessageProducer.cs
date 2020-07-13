// <copyright file="QueueMessageProducer.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
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
        private readonly BufferBlock<(QueueMessage, TimeSpan)> bufferBlock = new BufferBlock<(QueueMessage, TimeSpan)>();
        private readonly Task getMessagesTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducer"/> class.
        /// </summary>
        /// <param name="queue">A queue instance.</param>
        /// <param name="settings">Queue producer settings.</param>
        public QueueMessageProducer(IQueue queue, QueueMessageProducerSettings settings)
        {
            Queue = Requires.NotNull(queue, nameof(queue));
            this.getMessagesTask = Task.Run(async () =>
            {
                while (!DisposeToken.IsCancellationRequested)
                {
                    var queueMessages = await queue.GetMessagesAsync(settings.MessageCount, settings.VisibilityTimeout, settings.Timeout, DisposeToken);
                    if (queueMessages.Any())
                    {
                        foreach (var queueMessage in queueMessages)
                        {
                            await this.bufferBlock.SendAsync((queueMessage, settings.VisibilityTimeout), DisposeToken);
                        }
                    }
                }
            });
        }

        /// <inheritdoc/>
        public IQueue Queue { get; }

        /// <inheritdoc/>
        public ISourceBlock<(QueueMessage, TimeSpan)> Messages => this.bufferBlock;

        /// <inheritdoc/>
        protected override async Task DisposeInternalAsync()
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
}
