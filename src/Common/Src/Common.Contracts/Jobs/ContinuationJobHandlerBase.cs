// <copyright file="ContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Newtonsoft.Json;

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
        /// The continuation succeeded.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The continuation failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The continuation was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Continue to the next state.
        /// </summary>
        Continue,

        /// <summary>
        /// Retry this job message.
        /// </summary>
        Retry,
    }

    /// <summary>
    /// The continuation job payload.
    /// </summary>
    public class ContinuationJobPayload : JobPayload
    {
        /// <summary>
        /// Gets or sets a correlation id to track this payload.
        /// </summary>
        public Guid CorrelationId { get; set; }
    }

    /// <summary>
    /// The continuation job payload with current state.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    public abstract class ContinuationJobPayload<TState> : ContinuationJobPayload
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
    /// The continuation job result.
    /// </summary>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public class ContinuationJobResult<TState, TResult>
       where TResult : class
       where TState : struct, System.Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContinuationJobResult{TState, TResult}"/> class.
        /// </summary>
        /// <param name="resultState">The result state.</param>
        /// <param name="result">Optional result content.</param>
        /// <param name="nextState">Optional nextState.</param>
        /// <param name="isAutoNextState">Optional auot next option.</param>
        /// <param name="nextPayloadOptions">Optional payload options.</param>
        /// <param name="nextQueue">Optional next queue options.</param>
        public ContinuationJobResult(
            ContinuationJobPayloadResultState resultState,
            TResult result = null,
            TState? nextState = null,
            bool isAutoNextState = false,
            JobPayloadOptions nextPayloadOptions = null,
            (string, AzureLocation?)? nextQueue = null)
        {
            ResultState = resultState;
            Result = result;
            NextState = nextState;
            IsAutoNextState = isAutoNextState;
            NextPayloadOptions = nextPayloadOptions;
            NextQueue = nextQueue;
        }

        /// <summary>
        /// Gets the result state.
        /// </summary>
        public ContinuationJobPayloadResultState ResultState { get; }

        /// <summary>
        /// Gets the result value content.
        /// </summary>
        public TResult Result { get; }

        /// <summary>
        /// Gets the next state.
        /// </summary>
        public TState? NextState { get; }

        /// <summary>
        /// Gets the next payload options.
        /// </summary>
        public JobPayloadOptions NextPayloadOptions { get; }

        /// <summary>
        /// Gets a value indicating whether auot nhext is enabled.
        /// </summary>
        public bool IsAutoNextState { get; }

        /// <summary>
        /// Gets the next queue info.
        /// </summary>
        public (string, AzureLocation?)? NextQueue { get; }
    }

    /// <summary>
    /// Base class for our continuation job handlers.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class ContinuationJobHandlerBase<T, TState, TResult> : JobHandlerBase<T>
       where T : ContinuationJobPayload<TState>
       where TState : struct, System.Enum
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
        /// Gets the job queue producer factory.
        /// </summary>
        protected IJobQueueProducerFactory JobQueueProducerFactory { get; }

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

                // if continuation failed and it will be removed from the queue we failed our result
                if (jobCompleted.Status.HasFlag(JobCompletedStatus.Failed) && jobCompleted.Status.HasFlag(JobCompletedStatus.Removed))
                {
                    await CompleteJobAsync(job, ReturnFailed(), logger, cancellationToken);
                }
            };

            job.Completed += jobCompletedCallback;

            int currentStateIndex = Array.IndexOf(ContinuationStates, job.Payload.CurrentState);

            logger.AddBaseValue("CorrelationId", job.Payload.CorrelationId.ToString());
            var continueJobResult = await logger.OperationScopeAsync(
                "job_continuation_handler_continue",
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("JobContinuationState", job.Payload.CurrentState.ToString());
                    var result = await ContinueAsync(job, childLogger, cancellationToken);
                    if (result != null)
                    {
                        childLogger.FluentAddValue("JobContinuationResultState", result.ResultState)
                            .FluentAddValue("JobContinuationHasResult", result.Result != null)
                            .FluentAddValue("JobContinuationNextState", result.NextState);
                    }
                    else
                    {
                        childLogger.FluentAddValue("JobContinuationResultIsNull", true);
                    }

                    return result;
                }) ?? ReturnFailed();

            if (continueJobResult.ResultState == ContinuationJobPayloadResultState.Retry)
            {
                job.RetryTimeout = continueJobResult.NextPayloadOptions?.InitialVisibilityDelay ?? TimeSpan.FromSeconds(5);
            }
            else if (continueJobResult.ResultState == ContinuationJobPayloadResultState.Continue)
            {
                // move to next state and re queue
                TState? nextState = continueJobResult.NextState;
                if (!nextState.HasValue && continueJobResult.IsAutoNextState)
                {
                    nextState = ContinuationStates[currentStateIndex + 1];
                }

                if (nextState.HasValue)
                {
                    job.Payload.CurrentState = nextState.Value;
                }

                var queueInfo = GetQueueInfo(job, continueJobResult);
                await JobQueueProducerFactory.GetOrCreate(queueInfo.Item1, queueInfo.Item2).AddJobAsync(job.Payload, continueJobResult.NextPayloadOptions, logger, cancellationToken);
            }
            else
            {
                if (!(continueJobResult.ResultState == ContinuationJobPayloadResultState.Succeeded && continueJobResult.Result == null))
                {
                    await logger.OperationScopeAsync(
                        "job_continuation_complete",
                        (childLogger) => CompleteJobAsync(job, continueJobResult, logger, cancellationToken));
                }
            }
        }

        /// <summary>
        /// Return next state.
        /// </summary>
        /// <param name="state">Desired state to transition.</param>
        /// <param name="isAutoNextState">If auto next is desired.</param>
        /// <returns>Result tuple.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnNextState(TState? state = null, bool isAutoNextState = false)
        {
            return new ContinuationJobResult<TState, TResult>(ContinuationJobPayloadResultState.Continue, nextState: state, isAutoNextState: isAutoNextState);
        }

        /// <summary>
        /// Return a failure continuation.
        /// </summary>
        /// <param name="result">Result value.</param>
        /// <returns>Result tuple.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnFailed(TResult result = null)
        {
            return new ContinuationJobResult<TState, TResult>(ContinuationJobPayloadResultState.Failed, result: result);
        }

        /// <summary>
        /// Return a cancelled continuation.
        /// </summary>
        /// <param name="result">Result value.</param>
        /// <returns>Result tuple.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnCancelled(TResult result = null)
        {
            return new ContinuationJobResult<TState, TResult>(ContinuationJobPayloadResultState.Cancelled, result: result);
        }

        /// <summary>
        /// Return a retry continuation.
        /// </summary>
        /// <param name="retryTimeout">Optional retry timeout value.</param>
        /// <returns>Result tuple.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnRetry(TimeSpan? retryTimeout = null)
        {
            return new ContinuationJobResult<TState, TResult>(ContinuationJobPayloadResultState.Retry, nextPayloadOptions: new JobPayloadOptions() { InitialVisibilityDelay = retryTimeout });
        }

        /// <summary>
        /// Return a succeeded continuation.
        /// </summary>
        /// <param name="result">Result value.</param>
        /// <returns>Result tuple.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnSucceeded(TResult result = null)
        {
            return new ContinuationJobResult<TState, TResult>(ContinuationJobPayloadResultState.Succeeded, result: result);
        }

        /// <summary>
        /// Continue the next step.
        /// </summary>
        /// <param name="job">Job instance.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task<ContinuationJobResult<TState, TResult>> ContinueAsync(
            IJob<T> job,
            IDiagnosticsLogger logger,
            CancellationToken cancellationToken);

        private static (string, AzureLocation?) GetQueueInfo(IJob job, ContinuationJobResult<TState, TResult> continueJobResult)
        {
            return continueJobResult.NextQueue ?? (job.Queue.Id, null);
        }

        private Task CompleteJobAsync(IJob<T> job, ContinuationJobResult<TState, TResult> continueJobResult, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var resultPayload = new ContinuationJobPayloadResult<TState, TResult>()
            {
                CurrentState = continueJobResult.NextState.HasValue ? continueJobResult.NextState.Value : job.Payload.CurrentState,
                CompletionState = continueJobResult.ResultState,
                Result = continueJobResult.Result,
                CorrelationId = job.Payload.CorrelationId,
            };

            var queueInfo = GetQueueInfo(job, continueJobResult);
            return JobQueueProducerFactory.GetOrCreate(queueInfo.Item1, queueInfo.Item2).AddJobAsync(resultPayload, null, logger, cancellationToken);
        }
    }
#pragma warning restore SA1649
#pragma warning restore SA1402 // File may only contain a single type
}
