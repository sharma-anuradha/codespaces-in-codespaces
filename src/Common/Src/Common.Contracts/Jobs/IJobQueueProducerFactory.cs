// <copyright file="IJobQueueProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
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
        /// Retrieve the job queue producer metrics by each queue id.
        /// </summary>
        /// <returns>Dictionary of metrics for each queue id.</returns>
        Dictionary<string, Dictionary<string, IJobQueueProducerMetrics>> GetMetrics();

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
        /// Return the metrics processed by this queue.
        /// </summary>
        /// <returns>A dictionary for each type of job taf type beign procesed.</returns>
        Dictionary<string, IJobQueueProducerMetrics> GetMetrics();

        /// <summary>
        /// Add a new job into the queue.
        /// </summary>
        /// <param name="jobPayload">The job payload.</param>
        /// <param name="jobPayloadOptions">The job payload options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddJobAsync(JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Define job queue producer metrics.
    /// </summary>
    public interface IJobQueueProducerMetrics
    {
        /// <summary>
        /// Gets the number of jobs produced so far.
        /// </summary>
        public int Processed { get; }

        /// <summary>
        /// Gets the number of failures.
        /// </summary>
        public int Failures { get; }
    }
}
