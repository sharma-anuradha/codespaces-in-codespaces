// <copyright file="IJobQueueConsumerFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Definition of a job queue consumer factory.
    /// </summary>
    public interface IJobQueueConsumerFactory
    {
        /// <summary>
        /// Retrieve the current job consumer metrics.
        /// </summary>
        /// <returns>Dictionary of metrics for each queue id.</returns>
        Dictionary<(string, AzureLocation?), Dictionary<string, IJobHandlerMetrics>> GetMetrics();

        /// <summary>
        /// Create a new instance of a job queue consumer.
        /// </summary>
        /// <param name="queueId">The job queue id.</param>
        /// <param name="azureLocation">An optional Azure location.</param>
        /// <returns>Instance of anew job queue consumer.</returns>
        IJobQueueConsumer GetOrCreate(string queueId, AzureLocation? azureLocation = null);
    }

    /// <summary>
    /// A job queue consumer contract.
    /// </summary>
    public interface IJobQueueConsumer : IAsyncDisposable
    {
        /// <summary>
        /// Event fired when a job is created by this consumer instance.
        /// </summary>
        event Action<IJob> JobCreated;

        /// <summary>
        /// Event fired when a job handler metric is changed for payload type.
        /// </summary>
        event Action<string, IJobHandlerMetrics> JobHandlerMetricsUpdated;

        /// <summary>
        /// Return the current job handler metrics processed by this queue.
        /// </summary>
        /// <returns>A dictionary for each type of job taf type beign procesed.</returns>
        Dictionary<string, IJobHandlerMetrics> GetMetrics();

        /// <summary>
        /// Register a job handler to consume jobs.
        /// </summary>
        /// <typeparam name="T">Type of the payload.</typeparam>
        /// <param name="jobHandler">The job handler instance.</param>
        void RegisterJobHandler<T>(IJobHandler<T> jobHandler)
            where T : JobPayload;

        /// <summary>
        /// Start the job queue consumer processing.
        /// </summary>
        /// <param name="queueMessageProducerSettings">The queue message producer settings.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task StartAsync(QueueMessageProducerSettings queueMessageProducerSettings, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Define job handler metrics.
    /// </summary>
    public interface IJobHandlerMetrics
    {
        /// <summary>
        /// Gets the min input count on this queue.
        /// </summary>
        int MinInputCount { get; }

        /// <summary>
        /// Gets the max input count on this queue.
        /// </summary>
        int MaxInputCount { get; }

        /// <summary>
        /// Gets the accumulated job handler process time so far.
        /// </summary>
        TimeSpan ProcessTime { get; }

        /// <summary>
        /// Gets the accumulated job handler dequeued duration so far.
        /// </summary>
        TimeSpan DequeuedDuration { get; }

        /// <summary>
        /// Gets all the job process times.
        /// </summary>
        IReadOnlyCollection<TimeSpan> ProcessTimes { get; }

        /// <summary>
        /// Gets the number of jobs processed so far.
        /// </summary>
        int Processed { get; }

        /// <summary>
        /// Gets the number of failures.
        /// </summary>
        int Failures { get; }

        /// <summary>
        /// Gets the number of retries.
        /// </summary>
        int Retries { get; }

        /// <summary>
        /// Gets the number of cancellations.
        /// </summary>
        int Cancelled { get; }

        /// <summary>
        /// Gets the number of expired jobs.
        /// </summary>
        int Expired { get; }

        /// <summary>
        /// Gets the count of request to keep job payloads invisible.
        /// </summary>
        int KeepInvisibleCount { get; }
    }
}
