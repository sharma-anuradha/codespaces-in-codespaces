// <copyright file="EntityContinuationJobHandlerBase.cs" company="Microsoft">
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
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers
{
    public abstract class EntityContinuationJobHandlerBase<TRecord, TOperation, TPayload, TState, TResult> : ContinuationJobHandlerBase<TPayload, TState, TResult>, IJobHandlerTarget, IJobHandlerRegisterCallback
       where TPayload : EntityContinuationJobPayloadBase<TState>
       where TState : struct, System.Enum
       where TResult : EntityContinuationResult, new()
    {
        /// <summary>
        /// Max time a job payload would run after a fresh job payload is being created
        /// </summary>
        private static readonly TimeSpan MaxTimeJobPayloadRunAfterCreated = TimeSpan.FromHours(1);

        /// <summary>
        /// Define default job handler options.
        /// </summary>
        private static readonly JobHandlerOptions DefaultJobHandlerOptions = new JobHandlerOptions() { ExpireTimeout = MaxTimeJobPayloadRunAfterCreated };

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        protected EntityContinuationJobHandlerBase(
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null)
            : base(jobQueueProducerFactory, dataflowBlockOptions, DefaultJobHandlerOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        /// <param name="options">job handler options.</param>
        protected EntityContinuationJobHandlerBase(
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions,
            JobHandlerOptions options)
            : base(jobQueueProducerFactory, dataflowBlockOptions, options)
        {
        }

        /// <inheritdoc/>
        public abstract string QueueId { get; }

        /// <inheritdoc/>
        public virtual AzureLocation? Location => null;

        /// <inheritdoc/>
        public IJobHandler JobHandler => this;

        /// <summary>
        /// Gets the logger base name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the Operation that this handler is designed to manage.
        /// </summary>
        protected abstract TOperation Operation { get; }

        /// <summary>
        /// Gets the entity id
        /// </summary>
        protected abstract string EntityIdProperty { get; }

        /// <summary>
        /// Callback when the job handler is being registered.
        /// </summary>
        /// <param name="jobQueueConsumer">The job queue consumer.</param>
        public virtual void OnRegisterJobHandler(IJobQueueConsumer jobQueueConsumer)
        {
            // by default we will register the final result payload.
            jobQueueConsumer.RegisterJobPayloadHandler<ContinuationJobPayloadResult<TState, TResult>>(
                (payload, logger, ct) =>
                {
                    logger.FluentAddValue("CompletionState", payload.CompletionState)
                    .FluentAddValue("CurrentState", payload.CurrentState)
                    .FluentAddValue("ContinueJobHandler", LogBaseName);

                    return Task.CompletedTask;
                });
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

        protected static TResult ResultFromReason(string errorReason)
        {
            return new TResult() { ErrorReason = errorReason };
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<TState, TResult>> ContinueAsync(IJob<TPayload> job, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            // Note: to avoid get stuck in an infinity retries we will limit the amount of time when a job payload was created
            if ((DateTime.UtcNow - job.Created) > MaxTimeJobPayloadRunAfterCreated)
            {
                logger.FluentAddValue("ContinuationIsRunningTimeValid", false);
                return ReturnFailed("Maximum time waiting for continuation job to complete");
            }

            var payload = job.Payload;

            // Initial base values for the logger property.
            logger.FluentAddBaseValue(EntityIdProperty, payload.EntityId)
                .FluentAddValue("HandlerTriggerSource", payload.Reason);

            var record = await logger.TrackDurationAsync(
                "HandlerFetchReference", () => FetchReferenceAsync(payload, logger));

            if (!payload.IsInitialized)
            {
                return await InitializePayload(payload, record, logger);
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

        protected virtual async Task<ContinuationJobResult<TState, TResult>> InitializePayload(TPayload payload, IEntityRecordRef<TRecord> record, IDiagnosticsLogger logger)
        {
            // Update status
            await UpdateRecordStatusAsync(payload, record, OperationState.Initialized, "QueueOperation", logger);
            payload.IsInitialized = true;
            return ReturnNextState();
        }

        /// <summary>
        /// Convert to state an existing payload.
        /// </summary>
        /// <param name="payload">The payload instance.</param>
        /// <returns>The state enum.</returns>
        protected virtual TState GetStateFromPayload(TPayload payload)
        {
            return payload.CurrentState;
        }

        /// <summary>
        /// Convert to a continuation info.
        /// </summary>
        /// <param name="continuationResult">The deprectaed continuation info.</param>
        /// <param name="payload">The payload instance.</param>
        /// <returns>The continuation info.</returns>
        protected ContinuationJobResult<TState, TResult> ToContinuationInfo(ContinuationResult continuationResult, TPayload payload)
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
                result: ResultFromReason(continuationResult.ErrorReason),
                nextState: GetStateFromPayload(payload));
        }

        /// <summary>
        /// Preforms the steps required to build a response that indicates the contnuation should
        /// fail.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Fail continuation result.</returns>
        protected virtual async Task<ContinuationResult> FailOperationAsync(
            TPayload payload,
            IEntityRecordRef<TRecord> record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // Update compute to deal with the fact that storage has bombed
            await UpdateRecordStatusAsync(payload, record, OperationState.Failed, trigger, logger);

            // Setup failed result
            return new ContinuationResult { Status = OperationState.Failed };
        }

        /// <summary>
        /// Return a continuation info.
        /// </summary>
        /// <param name="payload">Payload instance.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        protected abstract Task<ContinuationJobResult<TState, TResult>> ContinueAsync(TPayload payload, IEntityRecordRef<TRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken);

        protected abstract Task<IEntityRecordRef<TRecord>> FetchReferenceAsync(TPayload payload, IDiagnosticsLogger logger);

        /// <summary>
        /// Update an environment record.
        /// </summary>
        /// <param name="payload">The continuation payload.</param>
        /// <param name="record">Reference to target resource.</param>
        /// <param name="mutateRecordCallback">Target callback which mutes the state.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected abstract Task<bool> UpdateRecordAsync(
            TPayload payload,
            IEntityRecordRef<TRecord> record,
            Func<TRecord, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger);

        protected abstract Task<bool> UpdateRecordStatusCallbackAsync(
            TPayload payload,
            IEntityRecordRef<TRecord> record,
            OperationState state,
            string trigger,
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
            TPayload payload,
            IEntityRecordRef<TRecord> record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var stateChanged = await UpdateRecordAsync(
                payload,
                record,
                (entityRecord, innerLogger) => UpdateRecordStatusCallbackAsync(payload, record, state, trigger, innerLogger),
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
            IEntityRecordRef<TRecord> record, IDiagnosticsLogger logger)
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
            TPayload payload,
            IEntityRecordRef<TRecord> record,
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
            TPayload payload,
            IEntityRecordRef<TRecord> record,
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
