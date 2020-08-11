// <copyright file="IJobQueueProducerFactoryHelpers.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Extension helpers for the IJobQueueProducer interface.
    /// </summary>
    public interface IJobQueueProducerFactoryHelpers
    {
        /// <summary>
        /// Return all available job queue producers on all regions.
        /// </summary>
        /// <param name="queueId">The queue id.</param>
        /// <returns>Enumerable jopb queue producers.</returns>
        IEnumerable<IJobQueueProducer> GetOrCreateAll(string queueId);

        /// <summary>
        /// Add a job to all available locations.
        /// </summary>
        /// <param name="queueId">The queue id.</param>
        /// <param name="jobPayload">Job payload instance.</param>
        /// <param name="jobPayloadOptions">Job payload options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddJobAllAsync(string queueId, JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken);
    }
}
