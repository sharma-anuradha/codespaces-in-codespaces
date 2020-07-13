// <copyright file="IJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Base interface for a job handler.
    /// </summary>
    public interface IJobHandler
    {
    }

    /// <summary>
    /// Definition of a job handler.
    /// </summary>
    /// <typeparam name="T">Type of the payload.</typeparam>
    public interface IJobHandler<T> : IJobHandler
        where T : JobPayload
    {
        /// <summary>
        /// Invoked by job consumers to process a IJob.
        /// </summary>
        /// <param name="job">The job type safe instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task HandleJobAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Definition of the supported job handler options.
    /// </summary>
    public interface IJobHandlerOptions
    {
        /// <summary>
        /// Gets the dataflow options.
        /// </summary>
        ExecutionDataflowBlockOptions DataflowBlockOptions { get; }
    }
}
