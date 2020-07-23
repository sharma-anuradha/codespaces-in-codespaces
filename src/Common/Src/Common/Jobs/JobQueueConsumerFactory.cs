// <copyright file="JobQueueConsumerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IJobQueueConsumerFactory interface.
    /// </summary>
    public class JobQueueConsumerFactory : IJobQueueConsumerFactory
    {
        private readonly IQueueMessageProducerFactory queueMessageProducerFactory;
        private readonly IDiagnosticsLogger logger;
        private readonly ConcurrentDictionary<string, JobQueueConsumer> jobQueueConsumers = new ConcurrentDictionary<string, JobQueueConsumer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumerFactory"/> class.
        /// </summary>
        /// <param name="queueMessageProducerFactory">Instance of a queue message producer factory.</param>
        /// <param name="logger">The logger instance.</param>
        public JobQueueConsumerFactory(IQueueMessageProducerFactory queueMessageProducerFactory, IDiagnosticsLogger logger)
        {
            this.queueMessageProducerFactory = Requires.NotNull(queueMessageProducerFactory, nameof(queueMessageProducerFactory));
            this.logger = Requires.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public IJobQueueConsumer GetOrCreate(string queueId)
        {
            return this.jobQueueConsumers.GetOrAdd(queueId, (id) => new JobQueueConsumer(this.queueMessageProducerFactory.Create(queueId), this.logger));
        }

        /// <inheritdoc/>
        public Dictionary<string, Dictionary<string, IJobHandlerMetrics>> GetMetrics()
        {
            return this.jobQueueConsumers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetMetrics());
        }
    }
}
