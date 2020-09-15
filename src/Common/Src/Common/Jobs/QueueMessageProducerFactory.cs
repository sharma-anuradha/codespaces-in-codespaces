// <copyright file="QueueMessageProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;
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
        public QueueMessageProducerFactory(IQueueFactory queueFactory)
        {
            QueueFactory = Requires.NotNull(queueFactory, nameof(queueFactory));
        }

        private IQueueFactory QueueFactory { get; }

        /// <inheritdoc/>
        public IQueueMessageProducer Create(string queueId, AzureLocation? azureLocation)
        {
            return new QueueMessageProducer(QueueFactory.GetOrCreate(queueId, azureLocation));
        }
    }
}
