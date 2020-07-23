// <copyright file="JobQueueProducerFactory.cs" company="Microsoft">
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
    /// Implements IJobQueueProducerFactory interface.
    /// </summary>
    public class JobQueueProducerFactory : IJobQueueProducerFactory
    {
        private readonly ConcurrentDictionary<string, JobQueueProducer> jobQueueProducer = new ConcurrentDictionary<string, JobQueueProducer>();
        private readonly IQueueFactory queueFactory;
        private readonly IDiagnosticsLogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueProducerFactory"/> class.
        /// </summary>
        /// <param name="queueFactory">A queue factory instance.</param>
        /// <param name="logger">Logger instance.</param>
        public JobQueueProducerFactory(IQueueFactory queueFactory, IDiagnosticsLogger logger)
        {
            this.queueFactory = Requires.NotNull(queueFactory, nameof(queueFactory));
            this.logger = Requires.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc/>
        public IJobQueueProducer GetOrCreate(string queueId)
        {
            var queue = this.queueFactory.GetOrCreate(queueId);
            return this.jobQueueProducer.GetOrAdd(queueId, (id) => new JobQueueProducer(queue, this.logger));
        }

        /// <inheritdoc/>
        public Dictionary<string, Dictionary<string, IJobQueueProducerMetrics>> GetMetrics()
        {
            return this.jobQueueProducer.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetMetrics());
        }
    }
}
