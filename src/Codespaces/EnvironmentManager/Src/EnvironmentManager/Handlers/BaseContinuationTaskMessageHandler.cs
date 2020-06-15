// <copyright file="BaseContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// Base Conutation Task Message Handler for operation based Tasks.
    /// </summary>
    /// <typeparam name="TI">Type of the target input.</typeparam>
    public abstract class BaseContinuationTaskMessageHandler<TI> : IContinuationTaskMessageHandler
        where TI : ContinuationOperationInput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseContinuationTaskMessageHandler{TI}"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        public BaseContinuationTaskMessageHandler(
            ICloudEnvironmentRepository cloudEnvironmentRepository)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
        }

        /// <summary>
        /// Gets the log base name.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the default Target that this handler targets.
        /// </summary>
        protected abstract string DefaultTarget { get; }

        /// <summary>
        /// Gets the Operation that this handler is designed to manage.
        /// </summary>
        protected abstract EnvironmentOperation Operation { get; }

        /// <summary>
        /// Gets the Environment Repository.
        /// </summary>
        protected ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        /// <inheritdoc/>
        public virtual bool CanHandle(ContinuationQueuePayload payload)
        {
            return payload.Target == DefaultTarget;
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                LogBaseName,
                async (childLogger) =>
                {
                    childLogger.FluentAddValue("HandlerOperation", Operation)
                        .FluentAddValue("HandlerType", GetType().Name)
                        .FluentAddValue("HandlerBasePreContinuationToken", input.ContinuationToken);

                    // Deals with invalid case
                    var typedInput = input as TI;
                    if (typedInput == null)
                    {
                        childLogger.FluentAddValue("HandlerInvalidInputType", true);

                        throw new NotSupportedException($"Provided input type does not match target input type - {typeof(TI)}");
                    }

                    childLogger.FluentAddBaseValue(EnvironmentLoggingPropertyConstants.EnvironmentId, typedInput.EnvironmentId)
                        .FluentAddValue("HandlerTriggerSource", typedInput.Reason);

                    // Core continue
                    var result = await InnerContinue(typedInput, childLogger);

                    childLogger.FluentAddValue("HandlerBasePostContinuationToken", result.NextInput?.ContinuationToken)
                        .FluentAddValue("HandlerBasePostStatus", result.Status)
                        .FluentAddValue("HandlerBasePostErrorReason", result.ErrorReason)
                        .FluentAddValue("HandlerBasePostRetryAfter", result.RetryAfter);

                    return result;
                });
        }

        /// <summary>
        /// Main continuation driver.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Next contiuation results.</returns>
        protected virtual async Task<ContinuationResult> InnerContinue(TI input, IDiagnosticsLogger logger)
        {
            var record = await logger.TrackDurationAsync(
                "HandlerFetchReference", () => FetchReferenceAsync(input, logger));

            // If first time through, queue things up
            if (string.IsNullOrEmpty(input.ContinuationToken))
            {
                // Queue operation to allow the rest of the continuation to occur
                return await logger.TrackDurationAsync(
                    "HandlerInitiallyQueueContinuation", () => InitiallyQueueContinuationAsync(input, record, logger));
            }

            // Run the core operation
            return await logger.TrackDurationAsync(
                "HandlerRunOperation", () => RunOperationAsync(input, record, logger));
        }

        /// <summary>
        /// Obtains a record state management object for a Resource.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Reference objec to the resource.</returns>
        protected virtual async Task<EnvironmentRecordRef> FetchReferenceAsync(TI input, IDiagnosticsLogger logger)
        {
            return await FetchReferenceAsync(input.EnvironmentId, logger);
        }

        /// <summary>
        /// Raw fetch of the a record state management object for a Resource.
        /// </summary>
        /// <param name="resourceId">Target Id that should be used to obtain the resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Reference objec to the resource.</returns>
        protected virtual async Task<EnvironmentRecordRef> FetchReferenceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            // Pull record
            var resource = await CloudEnvironmentRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                throw new CloudEnvironmentNotFoundException(resourceId);
            }

            return new EnvironmentRecordRef(resource);
        }

        /// <summary>
        /// Preforms the steps required to build a response that will put the operation request
        /// on the queue.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Queue continuation result.</returns>
        protected virtual async Task<ContinuationResult> InitiallyQueueContinuationAsync(
            TI input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update status
            await UpdateRecordStatusAsync(input, record, OperationState.Initialized, "QueueOperation", logger);

            // Build desired result setting up continuation
            input.ContinuationToken = BuildContinuationToken(input);

            return new ContinuationResult
            {
                Status = OperationState.Initialized,
                RetryAfter = TimeSpan.Zero,
                NextInput = input,
            };
        }

        /// <summary>
        /// Preforms the steps required to build a response that indicates the contnuation should
        /// run.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Run continuation result.</returns>
        protected virtual async Task<ContinuationResult> RunOperationAsync(
            TI input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update status to reflect that we are in progress
            await UpdateRecordStatusAsync(input, record, OperationState.InProgress, "PreRunOperation", logger);

            // Run core operation
            var threwException = false;
            var operationResult = (ContinuationResult)null;
            try
            {
                operationResult = await RunOperationCoreAsync(input, record, logger);
            }
            catch (Exception e)
            {
                // Log core details
                LogExceptionDetails("HandlerOperationException", e, logger);

                threwException = true;
            }

            // If we didn't get a result record error
            if (operationResult == null
                || operationResult.Status == OperationState.Failed
                || operationResult.Status == OperationState.Cancelled)
            {
                var failTrigger = threwException ? "Exception" : (operationResult == null ? "ResultNull" : (operationResult?.ErrorReason ?? operationResult.Status.ToString()));

                logger.FluentAddValue("HandlerFailedGetResultFromOperationException", threwException)
                    .FluentAddValue("HandlerFailedGetResultFromOperation", operationResult == null)
                    .FluentAddValue("HandlerFailedReason", operationResult?.ErrorReason);

                return await FailOperationAsync(input, record, $"PostRunOperation{failTrigger}", logger);
            }
            else
            {
                logger.FluentAddValue("HandlerOperationPostContinuationToken", operationResult.NextInput?.ContinuationToken)
                    .FluentAddValue("HandlerOperationPostStatus", operationResult.Status)
                    .FluentAddValue("HandlerOperationPostRetryAfter", operationResult.RetryAfter);

                // Update status to reflect compute result
                await UpdateRecordStatusAsync(input, record, operationResult.Status, "PostRunOperation", logger);

                return operationResult;
            }
        }

        /// <summary>
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="operationInput">Target operation input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target operations continuation result.</returns>
        protected abstract Task<ContinuationResult> RunOperationCoreAsync(TI operationInput, EnvironmentRecordRef record, IDiagnosticsLogger logger);

        /// <summary>
        /// Saves status update on a record.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="state">Target state that we want to move to.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected async Task<bool> UpdateRecordStatusAsync(
            TI input,
            EnvironmentRecordRef record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var stateChanged = await UpdateRecordAsync(
                input,
                record,
                (resource, innerLogger) =>
                {
                    // Get transition
                    var transition = FetchOperationTransition(input, record, innerLogger);

                    // Update transition
                    transition.Reason = input.Reason;
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
                    await FailOperationCleanupAsync(input, record, trigger, logger);
                }
            }

            return stateChanged;
        }

        /// <summary>
        /// Saves status update on a record.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="mutateRecordCallback">Target callback which mutes the state.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected async Task<bool> UpdateRecordAsync(
            TI input,
            EnvironmentRecordRef record,
            Func<CloudEnvironment, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger)
        {
            var stateChanged = false;

            // retry till we succeed
            await logger.RetryOperationScopeAsync(
                $"{LogBaseName}_status_update",
                async (innerLogger) =>
                {
                    // Obtain a fresh record.
                    record.Value = (await FetchReferenceAsync(input, innerLogger)).Value;

                    // Mutate record
                    stateChanged = await mutateRecordCallback(record.Value, innerLogger);

                    // Only need to update things if something has changed
                    if (stateChanged)
                    {
                        record.Value = await CloudEnvironmentRepository.UpdateAsync(record.Value, innerLogger.NewChildLogger());
                    }
                });

            return stateChanged;
        }

        /// <summary>
        /// Preforms the steps required to build a response that indicates the contnuation should
        /// fail.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="trigger">Trigger that caused the action.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Fail continuation result.</returns>
        protected virtual async Task<ContinuationResult> FailOperationAsync(
            TI input,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // Update compute to deal with the fact that storage has bombed
            await UpdateRecordStatusAsync(input, record, OperationState.Failed, trigger, logger);

            // Setup failed result
            return new ContinuationResult { Status = OperationState.Failed };
        }

        /// <summary>
        /// Cleanup action that should run post the a failure/exception occurring.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="trigger">Reason/cause for the delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns the core task.</returns>
        protected virtual async Task FailOperationCleanupAsync(
            TI input,
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
                    var cleanupResult = await FailOperationCleanupCoreAsync(input, record, trigger, logger.NewChildLogger());

                    logger.FluentAddValue("HandlerFailCleanupPostState", cleanupResult?.Status)
                        .FluentAddValue("HandlerFailCleanupPostContinuationToken", cleanupResult?.NextInput?.ContinuationToken);
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
        /// <param name="input">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="trigger">Reason/cause for the delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns status of action.</returns>
        protected virtual Task<ContinuationResult> FailOperationCleanupCoreAsync(
            TI input,
            EnvironmentRecordRef record,
            string trigger,
            IDiagnosticsLogger logger)
        {
            return Task.FromResult(new ContinuationResult { Status = OperationState.Succeeded });
        }

        /// <summary>
        /// Build the continuation, by default will be the `input.ResourceId`.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <returns>String that represents the continuation token.</returns>
        protected virtual string BuildContinuationToken(TI input)
        {
            return input.EnvironmentId.ToString();
        }

        /// <summary>
        /// Gets the transition for this opration.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Transition state.</returns>
        protected abstract TransitionState FetchOperationTransition(
            TI input,
            EnvironmentRecordRef record,
            IDiagnosticsLogger logger);

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
