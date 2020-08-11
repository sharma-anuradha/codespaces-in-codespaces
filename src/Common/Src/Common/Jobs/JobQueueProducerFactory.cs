// <copyright file="JobQueueProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IJobQueueProducerFactory interface.
    /// </summary>
    public class JobQueueProducerFactory : IJobQueueProducerFactory
    {
        private readonly ConcurrentDictionary<(string, AzureLocation?), JobQueueProducer> jobQueueProducer = new ConcurrentDictionary<(string, AzureLocation?), JobQueueProducer>();
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
        public IJobQueueProducer GetOrCreate(string queueId, AzureLocation? azureLocation)
        {
            var queue = this.queueFactory.GetOrCreate(queueId, azureLocation);
            return this.jobQueueProducer.GetOrAdd((queueId, azureLocation), (id) => new JobQueueProducer(queue, this.logger));
        }

        /// <inheritdoc/>
        public Dictionary<(string, AzureLocation?), Dictionary<string, IJobQueueProducerMetrics>> GetMetrics()
        {
            return this.jobQueueProducer.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetMetrics());
        }
    }
}
