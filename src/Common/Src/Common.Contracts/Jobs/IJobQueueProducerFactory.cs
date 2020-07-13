// <copyright file="IJobQueueProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// A Job Queue factory definition.
    /// </summary>
    public interface IJobQueueProducerFactory
    {
        /// <summary>
        /// Gets an existing or create a job producer.
        /// </summary>
        /// <param name="queueId">The queue id.</param>
        /// <returns>A job producer instance.</returns>
        IJobQueueProducer GetOrCreate(string queueId);
    }

    /// <summary>
    /// A Job Queue producer contract.
    /// </summary>
    public interface IJobQueueProducer
    {
        /// <summary>
        /// Add a new job into the queue.
        /// </summary>
        /// <param name="jobPayload">The job payload.</param>
        /// <param name="jobPayloadOptions">The job payload options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddJobAsync(JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken);
    }
}
