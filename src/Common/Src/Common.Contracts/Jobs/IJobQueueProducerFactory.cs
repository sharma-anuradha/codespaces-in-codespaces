// <copyright file="IJobQueueProducerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

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
        Dictionary<(string, AzureLocation?), Dictionary<string, IJobQueueProducerMetrics>> GetMetrics();

        /// <summary>
        /// Gets an existing or create a job producer.
        /// </summary>
        /// <param name="queueId">The queue id.</param>
        /// <param name="azureLocation">An optional Azure location.</param>
        /// <returns>A job producer instance.</returns>
        IJobQueueProducer GetOrCreate(string queueId, AzureLocation? azureLocation = null);
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
        /// <param name="logger">The logger diagnostic instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task that return the created Queue message.</returns>
        Task<QueueMessage> AddJobAsync(JobPayload jobPayload, JobPayloadOptions jobPayloadOptions, IDiagnosticsLogger logger, CancellationToken cancellationToken);
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

        /// <summary>
        /// total process time.
        /// </summary>
        public TimeSpan ProcessTime { get; }
    }
}
