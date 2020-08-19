// <copyright file="IJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// The job completed status.
    /// </summary>
    [Flags]
    public enum JobCompletedStatus
    {
        /// <summary>
        /// The job completed successfully.
        /// </summary>
        Succeeded = 1,

        /// <summary>
        /// The job failed with some exception error.
        /// </summary>
        Failed = 2,

        /// <summary>
        /// The job was removed from the queue.
        /// </summary>
        Removed = 4,

        /// <summary>
        /// The job will attempt a retry
        /// </summary>
        Retry = 8,

        /// <summary>
        /// The job expired.
        /// </summary>
        Expired = 16,

        /// <summary>
        /// The job was cancelled by a timeout.
        /// </summary>
        Cancelled = 32,

        /// <summary>
        /// The number of retries were exhausted.
        /// </summary>
        RetryExhausted = 64,

        /// <summary>
        /// The job was keeped invisble at least once.
        /// </summary>
        KeepInvisible = 128,

        /// <summary>
        /// A payload error
        /// </summary>
        PayloadError = 256,
    }

    /// <summary>
    /// The job completed status.
    /// </summary>
#pragma warning disable SA1649 // File name should match first type name
    public interface IJobCompleted
#pragma warning restore SA1649 // File name should match first type name
    {
        /// <summary>
        /// Gets the completion status.
        /// </summary>
        JobCompletedStatus Status { get; }

        /// <summary>
        /// Gets the error during the job handler processing.
        /// </summary>
        Exception Error { get; }

        /// <summary>
        /// Gets the number of times the job was needed to keep invisible.
        /// </summary>
        int KeepInvisibleCount { get; }
    }

    /// <summary>
    /// Job interface base definition.
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Event to report when the job is completed.
        /// </summary>
        event Func<IJobCompleted, CancellationToken, Task> Completed;

        /// <summary>
        /// Gets the unique job id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the source queue from which this job was processed.
        /// </summary>
        IQueue Queue { get; }

        /// <summary>
        /// Gets the visibility timeout.
        /// </summary>
        TimeSpan VisibilityTimeout { get; }

        /// <summary>
        /// Gets the creation date/time.
        /// </summary>
        DateTime Created { get; }

        /// <summary>
        /// Gets the number of retries.
        /// </summary>
        int Retries { get; }

        /// <summary>
        /// Update the job with a new visibility timeout.
        /// </summary>
        /// <param name="visibilityTimeout">The new visibility timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task UpdateVisibilityAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Type safe declaration of a job.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
    public interface IJob<T> : IJob
        where T : JobPayload
    {
        /// <summary>
        /// Gets the payload instance.
        /// </summary>
        T Payload { get; }
    }
}
