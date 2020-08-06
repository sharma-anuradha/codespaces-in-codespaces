// <copyright file="IJob.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Job interface base definition.
    /// </summary>
    public interface IJob
    {
        /// <summary>
        /// Gets the unique job id.
        /// </summary>
        string Id { get; }

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
        /// Update the job with a new visibility timoeut.
        /// </summary>
        /// <param name="visibilityTimeout">The new visibility timeout.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task UpdateAsync(TimeSpan visibilityTimeout, CancellationToken cancellationToken);
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
