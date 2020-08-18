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
        private readonly BufferBlock<(QueueMessage, TimeSpan)> bufferBlock = new BufferBlock<(QueueMessage, TimeSpan)>();
        private Task getMessagesTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducer"/> class.
        /// </summary>
        /// <param name="queue">A queue instance.</param>
        /// <param name="settings">Queue producer settings.</param>
        public QueueMessageProducer(IQueue queue, QueueMessageProducerSettings settings)
        {
            Queue = Requires.NotNull(queue, nameof(queue));
            Settings = Requires.NotNull(settings, nameof(settings));
        }

        /// <inheritdoc/>
        public IQueue Queue { get; }

        /// <inheritdoc/>
        public ISourceBlock<(QueueMessage, TimeSpan)> Messages => this.bufferBlock;

        private QueueMessageProducerSettings Settings { get; set; }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.getMessagesTask != null)
            {
                throw new InvalidOperationException("Already started");
            }

            this.getMessagesTask = StartInternalAsync(cancellationToken);
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

        private async Task StartInternalAsync(CancellationToken cancellationToken)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, DisposeToken))
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var queueMessages = await Queue.GetMessagesAsync(Settings.MessageCount, Settings.VisibilityTimeout, Settings.Timeout, cts.Token);
                    if (queueMessages.Any())
                    {
                        foreach (var queueMessage in queueMessages)
                        {
                            await this.bufferBlock.SendAsync((queueMessage, Settings.VisibilityTimeout), cts.Token);
                        }
                    }
                }
            }
        }
    }
}
