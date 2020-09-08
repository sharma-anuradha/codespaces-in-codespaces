// <copyright file="EnvironmentContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The environment continutaion job handler base class.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class EnvironmentContinuationJobHandlerBase<T, TState, TResult> : ContinuationJobHandlerBase<T, TState, TResult>, IJobHandlerTarget
       where T : EnvironmentContinuationInputBase<TState>
       where TState : struct, System.Enum
       where TResult : EnvironmentContinuationResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        /// <param name="options">job handler options.</param>
        protected EnvironmentContinuationJobHandlerBase(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null,
            JobHandlerOptions options = null)
            : base(jobQueueProducerFactory, dataflowBlockOptions, options)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
        }

        /// <inheritdoc/>
        public abstract string QueueId { get; }

        /// <inheritdoc/>
        public virtual AzureLocation? Location => null;

        /// <inheritdoc/>
        public void RegisterHandler(IJobQueueConsumer jobQueueConsumer)
        {
            jobQueueConsumer.RegisterJobHandler(this);
            OnRegisterJobHandler(jobQueueConsumer);
        }

        /// <summary>
        /// Gets the logger base name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the Operation that this handler is designed to manage.
        /// </summary>
        protected abstract EnvironmentOperation Operation { get; }

        /// <summary>
        /// Gets the env repository.
        /// </summary>
        protected ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        /// <summary>
        /// Start a continuation flow.
        /// </summary>
        /// <param name="payload">The initial payload.</param>
        /// <param name="jobPayloadOptions">Optional payload options.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>Completion task.</returns>
        public Task StartAsync(T payload, JobPayloadOptions jobPayloadOptions, CancellationToken cancellationToken)
        {
            Requires.NotNull(payload, nameof(payload));
            if (payload.IsInitialized)
            {
                throw new InvalidOperationException("payload already initialized");
            }

            return JobQueueProducerFactory.GetOrCreate(QueueId, Location).AddJobAsync(payload, jobPayloadOptions, cancellationToken);
        }

        /// <summary>
        /// Return failed state based on a reason string.
        /// </summary>
        /// <param name="errorReason">The error reason that cause the failure.</param>
        /// <returns>Continuation info.</returns>
        protected static ContinuationJobResult<TState, TResult> ReturnFailed(string errorReason)
        {
            return ReturnFailed(new TResult() { ErrorReason = errorReason });
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<TState, TResult>> ContinueAsync(IJob<T> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var payload = job.Payload;

            var record = await logger.TrackDurationAsync(
                "HandlerFetchReference", () => CloudEnvironmentRepository.FetchReferenceAsync(payload.EnvironmentId, logger));

            if (!payload.IsInitialized)
            {
                // Update status
                await UpdateRecordStatusAsync(payload, record, OperationState.Initialized, "QueueOperation", logger);
                payload.IsInitialized = true;
                return ReturnNextState();
            }

            // Update status to reflect that we are in progress
            await UpdateRecordStatusAsync(payload, record, OperationState.InProgress, "PreRunOperation", logger);

            ContinuationJobResult<TState, TResult> result = null;

            // Run core operation
            var threwException = false;
            try
            {
                result = await ContinueAsync(payload, record, logger, cancellationToken);
            }
            catch (Exception e)
            {
                // Log core details
                LogExceptionDetails("HandlerOperationException", e, logger);

                threwException = true;
            }

            // If we didn't get a result record error
            if (threwException
                || result.ResultState == ContinuationJobPayloadResultState.Failed
                || result.ResultState == ContinuationJobPayloadResultState.Cancelled)
            {
                var failTrigger = threwException ? "Exception" : result.Result?.ErrorReason;

                logger.FluentAddValue("HandlerFailedGetResultFromOperationException", threwException)
                    .FluentAddValue("HandlerFailedReason", result.Result?.ErrorReason);

                await UpdateRecordStatusAsync(payload, record, OperationState.Failed, $"PostRunOperation{failTrigger}", logger);
            }
            else
            {
                // Update status to reflect compute result
                await UpdateRecordStatusAsync(payload, record, result.ResultState == ContinuationJobPayloadResultState.Succeeded ? OperationState.Succeeded : OperationState.InProgress, "PostRunOperation", logger);
            }

            return result;
        }

        /// <summary>
        /// Convert to state an existing payload.
        /// </summary>
        /// <param name="payload">The payload instance.</param>
        /// <returns>The state enum.</returns>
        protected virtual TState GetStateFromPayload(T payload)
        {
            return payload.CurrentState;
        }

        /// <summary>
        /// Callback when the job handler is beign registered.
        /// </summary>
        /// <param name="jobQueueConsumer">The job queue consumer.</param>
        protected virtual void OnRegisterJobHandler(IJobQueueConsumer jobQueueConsumer)
        {
            jobQueueConsumer.RegisterJobPayloadHandler<ContinuationJobPayloadResult<TState, EnvironmentContinuationResult>>(
                (payload, logger, ct) =>
                {
                    logger.FluentAddValue("CompletionState", payload.CompletionState)
                    .FluentAddValue("CurrentState", payload.CurrentState)
                    .FluentAddValue("ContinueJobHandler", LogBaseName);

                    return Task.CompletedTask;
                });
        }

        /// <summary>
        /// Convert to a continuation info.
        /// </summary>
        /// <param name="continuationResult">The deprectaed continuation info.</param>
        /// <param name="payload">The payload instance.</param>
        /// <returns>The continuation info.</returns>
        protected ContinuationJobResult<TState, TResult> ToContinuationInfo(ContinuationResult continuationResult, T payload)
        {
            if (continuationResult.RetryAfter != default)
            {
                return new ContinuationJobResult<TState, TResult>(
                    ContinuationJobPayloadResultState.Retry,
                    nextPayloadOptions: new JobPayloadOptions() { InitialVisibilityDelay = continuationResult.RetryAfter });
            }

            ContinuationJobPayloadResultState continuationJobPayloadResultState;
            switch (continuationResult.Status)
            {
                case OperationState.Cancelled:
                    continuationJobPayloadResultState = ContinuationJobPayloadResultState.Cancelled;
                    break;
                case OperationState.Failed:
                    continuationJobPayloadResultState = ContinuationJobPayloadResultState.Failed;
                    break;
                case OperationState.Succeeded:
                    continuationJobPayloadResultState = ContinuationJobPayloadResultState.Succeeded;
                    break;
                case OperationState.InProgress:
                    continuationJobPayloadResultState = ContinuationJobPayloadResultState.Continue;
                    break;
                default:
                    throw new InvalidOperationException($"Status:{continuationResult.Status} not supported");
            }

            return new ContinuationJobResult<TState, TResult>(
                continuationJobPayloadResultState,
                result: new TResult() { ErrorReason = continuationResult.ErrorReason },
                nextState: GetStateFromPayload(payload));
        }

        /// <summary>
        /// Return a continuation info.
        /// </summary>
        /// <param name="payload">Payload instance.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task<ContinuationJobResult<TState, TResult>> ContinueAsync(T payload, EnvironmentRecordRef record, IDiagnosticsLogger logger, CancellationToken cancellationToken);

        /// <summary>
        /// Update an environment record.
        /// </summary>
        /// <param name="payload">The continuation payload.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="mutateRecordCallback">Target callback which mutes the state.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected Task<bool> UpdateRecordAsync(
            EnvironmentContinuationInputBase<TState> payload,
            EnvironmentRecordRef record,
            Func<CloudEnvironment, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger)
        {
            return CloudEnvironmentRepository.UpdateRecordAsync(payload.EnvironmentId, record, mutateRecordCallback, logger, LogBaseName);
        }

        /// <summary>
        /// Gets the transition for this opration.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Transition state.</returns>
        protected abstract TransitionState FetchOperationTransition(
            T payload,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger);

        /// <summary>
        /// Saves status update on a record.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="state">Target state that we want to move to.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected async Task<bool> UpdateRecordStatusAsync(
            T payload,
            EnvironmentRecordRef record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var stateChanged = await UpdateRecordAsync(
                payload,
                record,
                (resource, innerLogger) =>
                {
                    // Get transition
                    var transition = FetchOperationTransition(payload, record, innerLogger);

                    // Update transition
                    transition.Reason = payload.Reason;
                    var changed = transition.UpdateStatus(state, trigger);

                    return Task.FromResult(changed);
                },
                logger);

            // If the state was changed
            if (stateChanged)
            {
                // Trigger cleanup post fail
                if (state == OperationState.Failed)
                {
                    await FailOperationCleanupAsync(payload, record, trigger, logger);
                }
            }

            return stateChanged;
        }

        /// <summary>
        /// Indicates whether a compensating cleanup should happen as a result of a failed operation.
        /// </summary>
        /// <param name="record">Target record.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether cleanup should happen.</returns>
        protected virtual Task<bool> FailOperationShouldTriggerCleanupAsync(
            EnvironmentRecordRef record, IDiagnosticsLogger logger)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Core compensating action of we need to cleanup on failed operations.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="trigger">Reason/cause for the delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns status of action.</returns>
        protected virtual Task FailOperationCleanupCoreAsync(
            T payload,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup action that should run post the a failure/exception occurring.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="trigger">Reason/cause for the delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns the core task.</returns>
        protected virtual async Task FailOperationCleanupAsync(
            T payload,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // Check if cleanup needs to occur
            var shouldTriggerCleanup = await FailOperationShouldTriggerCleanupAsync(record, logger);

            trigger = $"{Operation}_{trigger}";

            logger.FluentAddValue("HandlerFailCleanupTriggered", shouldTriggerCleanup)
                .FluentAddValue("HandlerFailCleanupTrigger", trigger);

            // Only delete if we aren't already deleting
            if (shouldTriggerCleanup)
            {
                try
                {
                    await FailOperationCleanupCoreAsync(payload, record, trigger, logger.NewChildLogger());
                }
                catch (Exception e)
                {
                    logger.FluentAddValue("HandlerFailCleanupExceptionThrew", true)
                        .FluentAddValue("HandlerFailCleanupExceptionMessage", e.Message);

                    // Since we are swallowing, make sure we have a record at this level of the excpetion just in case
                    logger.NewChildLogger().LogException($"{LogBaseName}_failed_cleanup_error", e);
                }
            }
        }

        private void LogExceptionDetails(string propertyName, Exception e, IDiagnosticsLogger logger)
        {
            // Log core details
            logger.FluentAddValue($"{propertyName}Threw", true)
                .FluentAddValue($"{propertyName}Message", e.Message);

            // Since we are swallowing, make sure we have a record at this level of the excpetion just in case
            logger.NewChildLogger().LogException($"{LogBaseName}_operation_error", e);
        }
    }
}
