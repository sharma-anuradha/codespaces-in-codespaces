// <copyright file="BaseContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
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
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        public BaseContinuationTaskMessageHandler(
            IServiceProvider serviceProvider,
            IResourceRepository resourceRepository)
        {
            ServiceProvider = serviceProvider;
            ResourceRepository = resourceRepository;
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
        protected abstract ResourceOperation Operation { get; }

        /// <summary>
        /// Gets the Service Provider.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the Resource Repository.
        /// </summary>
        protected IResourceRepository ResourceRepository { get; }

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
                (childLogger) => InnerContinue(input, childLogger));
        }

        /// <summary>
        /// Main continuation driver.
        /// </summary>
        /// <param name="input">Base target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Next contiuation results.</returns>
        protected async Task<ContinuationResult> InnerContinue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("HandlerOperation", Operation)
                .FluentAddValue("HandlerType", GetType().Name)
                .FluentAddValue("HandlerBasePreContinuationToken", input.ContinuationToken);

            // Deals with invalid case
            var typedInput = input as TI;
            if (typedInput == null)
            {
                logger.FluentAddValue("HandlerInvalidInputType", true);

                throw new NotSupportedException($"Provided input type does not match target input type - {typeof(TI)}");
            }

            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, typedInput.ResourceId)
                .FluentAddValue("HandlerTriggerSource", typedInput.Reason)
                .FluentAddValue("HandlerOperationPreContinuationToken", typedInput.OperationInput?.ContinuationToken);

            // Core continue
            var result = await InnerContinue(typedInput, logger);

            logger.FluentAddValue("HandlerBasePostContinuationToken", result.NextInput?.ContinuationToken)
                .FluentAddValue("HandlerBasePostStatus", result.Status)
                .FluentAddValue("HandlerBasePostErrorReason", result.ErrorReason)
                .FluentAddValue("HandlerBasePostRetryAfter", result.RetryAfter);

            return result;
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
                "HandlerObtainReference", () => ObtainReferenceAsync(input, logger));

            // If first time through, queue things up
            if (string.IsNullOrEmpty(input.ContinuationToken))
            {
                // Short circuit things if thats whats happening
                if (await ShouldInitiallyHandleContinuationAsync(input, record, logger))
                {
                    // Custom logic to terminate things early if we are able to
                    return await logger.TrackDurationAsync(
                        "HandlerInitiallyHandleContinuationn", () => InitiallyHandleContinuationAsync(input, record, logger));
                }
                else
                {
                    // Queue operation to allow the rest of the continuation to occur
                    return await logger.TrackDurationAsync(
                        "HandlerInitiallyQueueContinuation", () => InitiallyQueueContinuationAsync(input, record, logger));
                }
            }

            // If we don't have the operation input build it
            if (input.OperationInput == null)
            {
                try
                {
                    // Get core input
                    input.OperationInput = await logger.TrackDurationAsync(
                        "HandlerBuildOperationInput", () => BuildOperationInputAsync(input, record, logger));
                }
                catch (Exception e) when (!(e is ContinuationTaskTemporarilyUnavailableException))
                {
                    // Log core details
                    LogExceptionDetails("HandlerOperationInputException", e, logger);

                    return await FailOperationAsync(input, record, "BuildOperationInputFailed", logger);
                }

                // If we were not able to build operation input fail
                if (input.OperationInput == null)
                {
                    logger.FluentAddValue("HandlerFailedBuildOperationInput", true);

                    return await FailOperationAsync(input, record, "PostBuildOperationInputFailed", logger);
                }
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
        protected virtual async Task<ResourceRecordRef> ObtainReferenceAsync(TI input, IDiagnosticsLogger logger)
        {
            return await FetchReferenceAsync(input.ResourceId, logger);
        }

        /// <summary>
        /// Raw fetch of the a record state management object for a Resource.
        /// </summary>
        /// <param name="resourceId">Target Id that should be used to obtain the resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Reference objec to the resource.</returns>
        protected virtual async Task<ResourceRecordRef> FetchReferenceAsync(Guid resourceId, IDiagnosticsLogger logger)
        {
            // Pull record
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.NewChildLogger());
            if (resource == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                throw new ResourceNotFoundException(resourceId);
            }

            return new ResourceRecordRef(resource, resourceId);
        }

        /// <summary>
        /// Signals whether operation should be initially handled or added to queue for
        /// further processing.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Whether we are short cirting the operaiton.</returns>
        protected virtual Task<bool> ShouldInitiallyHandleContinuationAsync(TI input, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Handler which runs if we are short circuiting the opertion by not adding to queue.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Required target input.</returns>
        protected virtual async Task<ContinuationResult> InitiallyHandleContinuationAsync(TI input, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            var resultStatus = OperationState.Succeeded;

            // Update status straight to target status
            await UpdateRecordStatusAsync(input, record, resultStatus, "HandleOperation", logger.NewChildLogger());

            return new ContinuationResult { Status = resultStatus };
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
            ResourceRecordRef record,
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
        /// Builds the input required for the target continuation.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Required target input.</returns>
        protected abstract Task<ContinuationInput> BuildOperationInputAsync(TI input, ResourceRecordRef record, IDiagnosticsLogger logger);

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
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update status to reflect that we are in progress
            await UpdateRecordStatusAsync(input, record, OperationState.InProgress, "PreRunOperation", logger);

            // Run core operation
            var threwException = false;
            var operationResult = (ContinuationResult)null;
            try
            {
                operationResult = await RunOperationCoreAsync(input, record, logger.NewChildLogger());
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
                var failTrigger = threwException ? "Exception" : (operationResult == null ? "ResultNull" : operationResult.Status.ToString());

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

                // Build desired result setting up next input
                input.OperationInput = operationResult.NextInput;
                return new ContinuationResult
                {
                    Status = operationResult.Status,
                    RetryAfter = operationResult.RetryAfter,
                    NextInput = input,
                };
            }
        }

        /// <summary>
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="operationInput">Target operation input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target operations continuation result.</returns>
        protected abstract Task<ContinuationResult> RunOperationCoreAsync(TI operationInput, ResourceRecordRef record, IDiagnosticsLogger logger);

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
            ResourceRecordRef record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var stateChanged = await UpdateRecordAsync(
                input,
                record,
                (resource) =>
                {
                    var changed = false;

                    // Determine what needs to be updated
                    if (Operation == ResourceOperation.Starting)
                    {
                        resource.StartingReason = input.Reason;
                        changed = resource.UpdateStartingStatus(state, trigger);
                    }
                    else if (Operation == ResourceOperation.Deleting)
                    {
                        resource.DeletingReason = input.Reason;
                        changed = resource.UpdateDeletingStatus(state, trigger);
                    }
                    else if (Operation == ResourceOperation.Provisioning)
                    {
                        resource.ProvisioningReason = input.Reason;
                        changed = resource.UpdateProvisioningStatus(state, trigger);
                    }
                    else if (Operation == ResourceOperation.Initializing)
                    {
                        resource.InitializationReason = input.Reason;
                        changed = resource.UpdateInitializationStatus(state, trigger);
                    }
                    else if (Operation == ResourceOperation.CleanUp)
                    {
                        resource.CleanupReason = input.Reason;
                        changed = resource.UpdateCleanupStatus(state, trigger);
                    }
                    else
                    {
                        throw new NotSupportedException($"Operation type is not supported - {Operation}");
                    }

                    return changed;
                },
                logger);

            // If the state was changed
            if (stateChanged)
            {
                // Trigger cleanup post fail
                if (state == OperationState.Failed)
                {
                    await FailOperationCleanupAsync(record, trigger, logger);
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
            ResourceRecordRef record,
            Func<ResourceRecord, bool> mutateRecordCallback,
            IDiagnosticsLogger logger)
        {
            var stateChanged = false;

            // retry till we succeed
            await logger.RetryOperationScopeAsync(
                $"{LogBaseName}_status_update",
                async (IDiagnosticsLogger innerLogger) =>
                {
                    // Obtain a fresh record.
                    record.Value = (await FetchReferenceAsync(record.ResourceId, logger)).Value;

                    // Mutate record
                    stateChanged = mutateRecordCallback(record.Value);

                    // Only need to update things if something has changed
                    if (stateChanged)
                    {
                        record.Value = await ResourceRepository.UpdateAsync(record.Value, innerLogger.NewChildLogger());
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
            ResourceRecordRef record,
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
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="trigger">Reason/cause for the delete.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returns the core task.</returns>
        protected async virtual Task FailOperationCleanupAsync(ResourceRecordRef record, string trigger, IDiagnosticsLogger logger)
        {
            var shouldTriggerDelete =
                (Operation == ResourceOperation.Provisioning) ||
                (Operation == ResourceOperation.Initializing) ||
                (Operation == ResourceOperation.Starting && record.Value.Type == ResourceType.ComputeVM);

            trigger = $"{Operation}_{trigger}";

            logger.FluentAddValue("HandlerFailCleanupTriggered", shouldTriggerDelete)
                .FluentAddValue("HandlerFailCleanupTrigger", trigger);

            // Only delete if we aren't already deleting
            if (shouldTriggerDelete)
            {
                try
                {
                    // Fetch instance
                    var resourceContinuationOperations = ServiceProvider.GetService<IResourceContinuationOperations>();

                    // Starts the delete workflow on the resource
                    var deleteResult = await resourceContinuationOperations.DeleteAsync(
                        Guid.Parse(record.Value.Id), trigger, logger.NewChildLogger());

                    logger.FluentAddValue("HandlerFailCleanupPostState", deleteResult?.Status)
                        .FluentAddValue("HandlerFailCleanupPostContinuationToken", deleteResult?.NextInput?.ContinuationToken);
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
        /// Build the continuation, by default will be the `input.ResourceId`.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <returns>String that represents the continuation token.</returns>
        protected virtual string BuildContinuationToken(TI input)
        {
            return input.ResourceId.ToString();
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
