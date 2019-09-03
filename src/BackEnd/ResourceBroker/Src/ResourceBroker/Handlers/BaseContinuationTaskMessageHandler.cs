// <copyright file="BaseContinuationTaskMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
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
        private const string LogBaseName = ResourceLoggingConstants.BaseContinuationTaskMessageHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseContinuationTaskMessageHandler{TI}"/> class.
        /// </summary>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        public BaseContinuationTaskMessageHandler(IResourceRepository resourceRepository)
        {
            ResourceRepository = resourceRepository;
        }

        /// <summary>
        /// Gets the default Target that this handler targets.
        /// </summary>
        protected abstract string DefaultTarget { get; }

        /// <summary>
        /// Gets the Operation that this handler is designed to manage.
        /// </summary>
        protected abstract ResourceOperation Operation { get; }

        /// <summary>
        /// Gets the Resource Repository.
        /// </summary>
        protected IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        public virtual bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == DefaultTarget;
        }

        /// <inheritdoc/>
        public Task<ContinuationResult> Continue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(LogBaseName, () => InnerContinue(input, logger));
        }

        /// <summary>
        /// Main continuation driver.
        /// </summary>
        /// <param name="input">Base target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Next contiuation results.</returns>
        protected Task<ContinuationResult> InnerContinue(ContinuationInput input, IDiagnosticsLogger logger)
        {
            logger.FluentAddValue("HandlerOperation", Operation);

            // Deals with invalid case
            var typedInput = input as TI;
            if (typedInput == null)
            {
                logger.FluentAddValue("HandlerInvalidInputType", true);

                throw new NotSupportedException($"Provided input type does not match target input type - {typeof(TI)}");
            }

            return InnerContinue(typedInput, logger);
        }

        /// <summary>
        /// Main continuation driver.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Next contiuation results.</returns>
        protected virtual async Task<ContinuationResult> InnerContinue(TI input, IDiagnosticsLogger logger)
        {
            var timer = logger.TrackDuration("HandlerObtainReferenceDuration");
            var record = await ObtainReferenceAsync(input, logger);
            timer.Dispose();

            // If first time through, queue things up
            if (string.IsNullOrEmpty(input.ContinuationToken))
            {
                using (logger.TrackDuration("HandlerQueueOperationDuration"))
                {
                    return await QueueOperationAsync(input, record, logger);
                }
            }

            // If we don't have the operation input build it
            if (input.OperationInput == null)
            {
                timer = logger.TrackDuration("HandlerBuildOperationInputDuration");
                input.OperationInput = await BuildOperationInputAsync(input, record, logger);
                timer.Dispose();

                // If we were not able to build operation input fail
                if (input.OperationInput == null)
                {
                    logger.FluentAddValue("HandlerFailedBuildOperationInput", true);

                    return await FailOperationAsync(input, record, logger);
                }
            }

            // Run the core operation
            using (logger.TrackDuration("HandlerRunOperationDuration"))
            {
                return await RunOperationAsync(input, record, logger);
            }
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
        /// Triggers the run operation on the target continuation.
        /// </summary>
        /// <param name="operationInput">Target operation input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Target operations continuation result.</returns>
        protected abstract Task<ContinuationResult> RunOperationAsync(ContinuationInput operationInput, ResourceRecordRef record, IDiagnosticsLogger logger);

        /// <summary>
        /// Build the continuation, by default will be the `input.ResourceId`.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <returns>String that represents the continuation token.</returns>
        protected virtual string BuildContinuationToken(TI input)
        {
            return input.ResourceId.ToString();
        }

        /// <summary>
        /// Obtains a record state management object for a Resource.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Reference objec to the resource.</returns>
        protected virtual async Task<ResourceRecordRef> ObtainReferenceAsync(TI input, IDiagnosticsLogger logger)
        {
            // Ensure that we have a resource to work with
            if (!input.ResourceId.HasValue)
            {
                logger.FluentAddValue("HandlerFailedToFindInputResourceId", true);

                throw new NotSupportedException("Resource id hasn't been been provided when obtaining resource record.");
            }

            return await FetchReferenceAsync(input.ResourceId.Value, logger);
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
            var resource = await ResourceRepository.GetAsync(resourceId.ToString(), logger.WithValues(new LogValueSet()));
            if (resource == null)
            {
                logger.FluentAddValue("HandlerFailedToFindResource", true);

                throw new ResourceNotFoundException(resourceId);
            }

            return new ResourceRecordRef(resource);
        }

        /// <summary>
        /// Saves status update on a record.
        /// </summary>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="state">Target state that we want to move to.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Returned task.</returns>
        protected async Task SaveStatusAsync(ResourceRecordRef record, OperationState state, IDiagnosticsLogger logger)
        {
            var stateChanged = false;

            // Determin what needs to be updated
            if (Operation == ResourceOperation.Starting)
            {
                stateChanged = record.Value.UpdateStartingStatus(state);
            }
            else if (Operation == ResourceOperation.Deleting)
            {
                stateChanged = record.Value.UpdateDeletingStatus(state);
            }
            else if (Operation == ResourceOperation.Provisioning)
            {
                stateChanged = record.Value.UpdateProvisioningStatus(state);
            }
            else
            {
                throw new NotSupportedException($"Operation type is not selected - {Operation}");
            }

            // Only need to update things if something has changed
            if (stateChanged)
            {
                record.Value = await ResourceRepository.UpdateAsync(record.Value, logger.WithValues(new LogValueSet()));
            }
        }

        /// <summary>
        /// Preforms the steps required to build a response that will put the operation request
        /// on the queue.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Queue continuation result.</returns>
        protected virtual async Task<ContinuationResult> QueueOperationAsync(
            TI input,
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update status
            await SaveStatusAsync(record, OperationState.Initialized, logger);

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
        /// fail.
        /// </summary>
        /// <param name="input">Target input.</param>
        /// <param name="record">Referene to target resource.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Fail continuation result.</returns>
        protected virtual async Task<ContinuationResult> FailOperationAsync(
            TI input,
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update compute to deal with the fact that storage has bombed
            await SaveStatusAsync(record, OperationState.Failed, logger);

            // Setup failed result
            return new ContinuationResult { Status = OperationState.Failed };
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
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Update status to reflect that we are in progress
            await SaveStatusAsync(record, OperationState.InProgress, logger);

            // Run core operation
            var operationResult = await RunOperationAsync(input.OperationInput, record, logger.WithValues(new LogValueSet()));

            // If we didn't get a result record error
            if (operationResult == null)
            {
                logger.FluentAddValue("HandlerFailedGetResultFromOperation", true);

                return await FailOperationAsync(input, record, logger);
            }

            // Update status to reflect compute result
            await SaveStatusAsync(record, operationResult.Status, logger);

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
}
