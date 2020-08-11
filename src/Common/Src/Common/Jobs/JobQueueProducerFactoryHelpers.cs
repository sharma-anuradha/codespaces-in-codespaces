// <copyright file="JobQueueProducerFactoryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs
{
    /// <summary>
    /// Implements IJobQueueProducerFactoryHelpers.
    /// </summary>
    public class JobQueueProducerFactoryHelpers : IJobQueueProducerFactoryHelpers
    {
        private readonly IJobQueueProducerFactory jobQueueProducerFactory;
        private readonly IControlPlaneInfo controlPlaneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueProducerFactoryHelpers"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">The job queue producer factory instance.</param>
        /// <param name="controlPlaneInfo">The control plane info instance.</param>
        public JobQueueProducerFactoryHelpers(
            IJobQueueProducerFactory jobQueueProducerFactory,
            IControlPlaneInfo controlPlaneInfo)
        {
            this.jobQueueProducerFactory = Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));
            this.controlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
        }

        /// <inheritdoc/>
        public IEnumerable<IJobQueueProducer> GetOrCreateAll(string queueId)
        {
            return this.controlPlaneInfo.AllStamps.Select(kvp => this.jobQueueProducerFactory.GetOrCreate(queueId, kvp.Key));
        }

        /// <inheritdoc/>
        public async Task AddJobAllAsync(string queueId, JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken)
        {
            await Task.WhenAll(GetOrCreateAll(queueId).Select(jobQueueProducer => jobQueueProducer.AddJobAsync(jobPayload, jobPayloadOptions, cancellationToken)));
        }
    }
}
