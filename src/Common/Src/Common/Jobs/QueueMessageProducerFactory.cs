// <copyright file="QueueMessageProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IQueueMessageProducerFactory interface.
    /// </summary>
    public class QueueMessageProducerFactory : IQueueMessageProducerFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueMessageProducerFactory"/> class.
        /// </summary>
        /// <param name="queueFactory">A queue factroy instance.</param>
        /// <param name="queueMessageProducerSettings">Settings to apply on each queue message producer.</param>
        public QueueMessageProducerFactory(IQueueFactory queueFactory, QueueMessageProducerSettings queueMessageProducerSettings = null)
        {
            QueueFactory = Requires.NotNull(queueFactory, nameof(queueFactory));
            Settings = queueMessageProducerSettings ?? QueueMessageProducerSettings.Default;
        }

        private IQueueFactory QueueFactory { get; }

        private QueueMessageProducerSettings Settings { get; }

        /// <inheritdoc/>
        public IQueueMessageProducer Create(string queueId)
        {
            return new QueueMessageProducer(QueueFactory.GetOrCreate(queueId), Settings);
        }
    }
}
