// <copyright file="ResourceContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// The environment continutaion job handler base class.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class ResourceContinuationJobHandlerBase<TPayload, TState, TResult> : EntityContinuationJobHandlerBase<ResourceRecord, ResourceOperation, TPayload, TState, TResult>, IJobHandlerTarget, IJobHandlerRegisterCallback
       where TPayload : EntityContinuationJobPayloadBase<TState>
       where TState : struct, System.Enum
       where TResult : EntityContinuationResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        protected ResourceContinuationJobHandlerBase(
            IServiceProvider serviceProvider,
            IResourceRepository resourceRepository,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null)
            : base(jobQueueProducerFactory, dataflowBlockOptions)
        {
            ServiceProvider = serviceProvider;
            ResourceRepository = resourceRepository;
            ResourceStateManager = resourceStateManager;
        }

        /// <summary>
        /// Gets the Service Provider.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        /// Gets the Resource Repository.
        /// </summary>
        protected IResourceRepository ResourceRepository { get; }

        /// <inheritdoc/>
        protected override string EntityIdProperty => ResourceLoggingPropertyConstants.ResourceId;

        private IResourceStateManager ResourceStateManager { get; }

        /// <inheritdoc/>
        protected override async Task<bool> UpdateRecordAsync(
            TPayload payload,
            IEntityRecordRef<ResourceRecord> record,
            Func<ResourceRecord, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger)
        {
            var stateChanged = false;

            // retry till we succeed
            await logger.RetryOperationScopeAsync(
                $"{LogBaseName}_status_update",
                async (innerLogger) =>
                {
                    // Obtain a fresh record.
                    record.Value = (await FetchReferenceAsync(Guid.Parse(record.Value.Id), innerLogger)).Value;

                    // Mutate record
                    stateChanged = await mutateRecordCallback(record.Value, innerLogger);

                    // Only need to update things if something has changed
                    if (stateChanged)
                    {
                        record.Value = await ResourceRepository.UpdateAsync(record.Value, innerLogger.NewChildLogger());
                    }
                });

            return stateChanged;
        }

        /// <inheritdoc/>
        protected override async Task<IEntityRecordRef<ResourceRecord>> FetchReferenceAsync(TPayload payload, IDiagnosticsLogger logger)
        {
            return await FetchReferenceAsync(payload.EntityId, logger);
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

            return new ResourceRecordRef(resource);
        }

        /// <inheritdoc/>
        protected override async Task<bool> UpdateRecordStatusCallbackAsync(
            TPayload payload,
            IEntityRecordRef<ResourceRecord> record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            var resource = record.Value;
            var changed = false;

            // Determine what needs to be updated
            if (Operation == ResourceOperation.StartEnvironment
                || Operation == ResourceOperation.StartArchiving)
            {
                resource.StartingReason = payload.Reason;
                changed = resource.UpdateStartingStatus(state, trigger);
            }
            else if (Operation == ResourceOperation.Deleting)
            {
                resource.DeletingReason = payload.Reason;
                changed = resource.UpdateDeletingStatus(state, trigger);
            }
            else if (Operation == ResourceOperation.Provisioning)
            {
                resource.ProvisioningReason = payload.Reason;
                changed = resource.UpdateProvisioningStatus(state, trigger);
            }
            else if (Operation == ResourceOperation.CleanUp)
            {
                resource.CleanupReason = payload.Reason;
                changed = resource.UpdateCleanupStatus(state, trigger);
            }
            else
            {
                throw new NotSupportedException($"Operation type is not supported - {Operation}");
            }

            if (changed && (Operation == ResourceOperation.Provisioning && state == OperationState.Succeeded))
            {
                if (record.Value.Type == ResourceType.StorageFileShare || record.Value.Type == ResourceType.KeyVault)
                {
                    // Try to assign resource to queued requests
                    record.Value = await ResourceStateManager.MarkResourceReady(record.Value, "ProvisioningSucceeded", logger.NewChildLogger());
                }
            }

            return changed;
        }
    }
}
