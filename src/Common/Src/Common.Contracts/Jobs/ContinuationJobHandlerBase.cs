// <copyright file="ContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts
{
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649

    /// <summary>
    /// The continuation job payload result state.
    /// </summary>
    public enum ContinuationJobPayloadResultState
    {
        /// <summary>
        /// None.
        /// </summary>
        None,

        /// <summary>
        /// The continuation succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The continuation failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Invalid operation on the job handler.
        /// </summary>
        InvalidOperation,
    }

    /// <summary>
    /// The continuation job payload.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    public abstract class ContinuationJobPayload<TState> : JobPayload
        where TState : System.Enum
    {
        /// <summary>
        /// Gets or sets the current state of this continuation payload.
        /// </summary>
        public TState CurrentState { get; set; }
    }

    /// <summary>
    /// The continuation job payload result.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public class ContinuationJobPayloadResult<TState, TResult> : ContinuationJobPayload<TState>
        where TState : System.Enum
        where TResult : class
    {
        /// <summary>
        /// Gets or sets the completion state.
        /// </summary>
        public ContinuationJobPayloadResultState CompletionState { get; set; }

        /// <summary>
        /// Gets or sets the result instance.
        /// </summary>
        public TResult Result { get; set; }
    }

    /// <summary>
    /// Base class for our continuation job handlers.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class ContinuationJobHandlerBase<T, TState, TResult> : JobHandlerBase<T>
       where T : ContinuationJobPayload<TState>
       where TState : System.Enum
       where TResult : class
    {
        private static readonly TState[] ContinuationStates = (TState[])Enum.GetValues(typeof(TState));

        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        /// <param name="options">job handler options.</param>
        protected ContinuationJobHandlerBase(
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null,
            JobHandlerOptions options = null)
            : base(dataflowBlockOptions, options)
        {
            JobQueueProducerFactory = Requires.NotNull(jobQueueProducerFactory, nameof(jobQueueProducerFactory));
        }

        /// <summary>
        /// Gets the completion queue id.
        /// </summary>
        protected virtual string CompletedQueueId => null;

        private IJobQueueProducerFactory JobQueueProducerFactory { get; }

        /// <inheritdoc/>
        public override JobHandlerOptions GetJobOptions(IJob<T> job)
        {
            var handlerOptions = base.GetJobOptions(job);
            return handlerOptions ?? new JobHandlerOptions() { MaxHandlerRetries = 1 };
        }

        /// <inheritdoc/>
        public override async Task HandleJobAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            Func<IJobCompleted, CancellationToken, Task> jobCompletedCallback = null;
            jobCompletedCallback = async (jobCompleted, ct) =>
            {
                job.Completed -= jobCompletedCallback;

                // if cometion failed and will be removed from the queue we failed our result
                if (jobCompleted.Status.HasFlag(JobCompletedStatus.Failed) && jobCompleted.Status.HasFlag(JobCompletedStatus.Removed))
                {
                    await CompleteJobAsync(job, null, ContinuationJobPayloadResultState.Failed, cancellationToken);
                }
            };

            job.Completed += jobCompletedCallback;

            int currentStateIndex = Array.IndexOf(ContinuationStates, job.Payload.CurrentState);

            var continueResult = await logger.OperationScopeAsync(
                "job_continuation_handler_continue",
                (childLogger) =>
                {
                    childLogger.FluentAddValue("JobContinuationState", job.Payload.CurrentState.ToString());
                    return ContinueAsync(job.Payload, childLogger, cancellationToken);
                });

            if (continueResult.Item2 != ContinuationJobPayloadResultState.None)
            {
                await CompleteJobAsync(job, continueResult.Item1, continueResult.Item2, cancellationToken);
            }
            else
            {
                if ((currentStateIndex + 1) == ContinuationStates.Length)
                {
                    await CompleteJobAsync(job, null, ContinuationJobPayloadResultState.InvalidOperation, cancellationToken);
                }
                else
                {
                    // move to next state and re queue
                    job.Payload.CurrentState = ContinuationStates[currentStateIndex + 1];
                    await JobQueueProducerFactory.GetOrCreate(ContinuationQueueId(job.Payload.CurrentState) ?? GetDefaultQueueId(job)).AddJobAsync(job.Payload, null, cancellationToken);
                }
            }
         }

        /// <summary>
        /// Return the queue id of the next continuation.
        /// </summary>
        /// <param name="state">Next state of the continuation.</param>
        /// <returns>The queue id or null if a default value is enough.</returns>
        protected virtual string ContinuationQueueId(TState state) => null;

        /// <summary>
        /// Continue the next step.
        /// </summary>
        /// <param name="payload">Current payload value.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task<(TResult, ContinuationJobPayloadResultState)> ContinueAsync(
            T payload,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken);

        private static string GetDefaultQueueId(IJob job)
        {
            return job.Queue.Id;
        }

        private Task CompleteJobAsync(IJob<T> job, TResult result, ContinuationJobPayloadResultState state, CancellationToken cancellationToken)
        {
            var resultPayload = new ContinuationJobPayloadResult<TState, TResult>() { CurrentState = job.Payload.CurrentState, CompletionState = state, Result = result };
            return JobQueueProducerFactory.GetOrCreate(CompletedQueueId ?? GetDefaultQueueId(job)).AddJobAsync(resultPayload, null, cancellationToken);
        }
    }
#pragma warning restore SA1649
#pragma warning restore SA1402 // File may only contain a single type
}
