// <copyright file="JobQueueConsumerFactory.cs" company="Microsoft">
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
    /// Implements IJobQueueConsumerFactory interface.
    /// </summary>
    public class JobQueueConsumerFactory : IJobQueueConsumerFactory
    {
        private readonly IDiagnosticsLogger logger;
        private readonly ConcurrentDictionary<(string, AzureLocation?), JobQueueConsumer> jobQueueConsumers = new ConcurrentDictionary<(string, AzureLocation?), JobQueueConsumer>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueConsumerFactory"/> class.
        /// </summary>
        /// <param name="queueFactory">A queue factroy instance.</param>
        /// <param name="logger">The logger instance.</param>
        public JobQueueConsumerFactory(IQueueFactory queueFactory, IDiagnosticsLogger logger)
        {
            QueueFactory = Requires.NotNull(queueFactory, nameof(queueFactory));
            this.logger = Requires.NotNull(logger, nameof(logger));
        }

        private IQueueFactory QueueFactory { get; }

        /// <inheritdoc/>
        public IJobQueueConsumer GetOrCreate(string queueId, AzureLocation? azureLocation)
        {
            return this.jobQueueConsumers.GetOrAdd((queueId, azureLocation), (id) => new JobQueueConsumer(QueueFactory.GetOrCreate(queueId, azureLocation), this.logger));
        }

        /// <inheritdoc/>
        public Dictionary<(string, AzureLocation?), Dictionary<string, IJobHandlerMetrics>> GetMetrics()
        {
            return this.jobQueueConsumers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetMetrics());
        }
    }
}
