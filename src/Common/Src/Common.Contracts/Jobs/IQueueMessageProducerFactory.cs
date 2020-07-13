// <copyright file="IQueueMessageProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks.Dataflow;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// A queue message producer factory definition.
    /// </summary>
    public interface IQueueMessageProducerFactory
    {
        /// <summary>
        /// Create a queue message producer.
        /// </summary>
        /// <param name="queueId">The queue id.</param>
        /// <returns>Instance of a queue message producer.</returns>
        IQueueMessageProducer Create(string queueId);
    }

    /// <summary>
    /// A queue message producer definition.
    /// </summary>
    public interface IQueueMessageProducer
    {
        /// <summary>
        /// Gets the underlying queue used.
        /// </summary>
        IQueue Queue { get; }

        /// <summary>
        /// Gets the message source block.
        /// </summary>
        ISourceBlock<(QueueMessage, TimeSpan)> Messages { get; }
    }
}
