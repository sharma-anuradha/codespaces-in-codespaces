// <copyright file="IJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
    /// <summary>
    /// Base interface for a job handler.
    /// </summary>
    public interface IJobHandler
    {
        /// <summary>
        /// Gets the dataflow options.
        /// </summary>
        ExecutionDataflowBlockOptions DataflowBlockOptions { get; }
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

        /// <summary>
        /// Get the job handler options to be used for a job.
        /// </summary>
        /// <param name="job">The job type safe instance.</param>
        /// <returns>Handler Options to use.</returns>
        JobHandlerOptions GetJobOptions(IJob<T> job);
    }

    /// <summary>
    /// Job handler target that define a queue id and location.
    /// </summary>
    public interface IJobHandlerTarget
    {
        /// <summary>
        /// The job handler to register.
        /// </summary>
        IJobHandler JobHandler { get; }

        /// <summary>
        /// Gets the queue id that this job handler target.
        /// </summary>
        string QueueId { get; }

        /// <summary>
        /// Gets the location of the queue.
        /// </summary>
        AzureLocation? Location { get; }
    }

    /// <summary>
    /// Contract to implement if a job handler want to be notified .
    /// </summary>
    public interface IJobHandlerRegisterCallback
    {
        /// <summary>
        /// Callback when the job handler is being registered.
        /// </summary>
        /// <param name="jobQueueConsumer">The job queue consumer.</param>
        void OnRegisterJobHandler(IJobQueueConsumer jobQueueConsumer);
    }
}
