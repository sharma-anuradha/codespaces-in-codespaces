// <copyright file="EnvironmentContinuationJobHandlerBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Handlers
{
    /// <summary>
    /// The environment continutaion job handler base class.
    /// </summary>
    /// <typeparam name="TPayload">The payload type.</typeparam>
    /// <typeparam name="TState">Type of the state enums.</typeparam>
    /// <typeparam name="TResult">Type of the result.</typeparam>
    public abstract class EnvironmentContinuationJobHandlerBase<TPayload, TState, TResult> : EntityContinuationJobHandlerBase<CloudEnvironment, EnvironmentOperation, TPayload, TState, TResult>, IJobHandlerTarget, IJobHandlerRegisterCallback
       where TPayload : EntityContinuationJobPayloadBase<TState>
       where TState : struct, System.Enum
       where TResult : EntityContinuationResult, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentContinuationJobHandlerBase{T, TState, TResult}"/> class.
        /// </summary>
        /// <param name="cloudEnvironmentRepository">Cloud Environment Repository to be used.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        /// <param name="dataflowBlockOptions">Dataflow execution options.</param>
        protected EnvironmentContinuationJobHandlerBase(
            ICloudEnvironmentRepository cloudEnvironmentRepository,
            IJobQueueProducerFactory jobQueueProducerFactory,
            ExecutionDataflowBlockOptions dataflowBlockOptions = null)
            : base(jobQueueProducerFactory, dataflowBlockOptions)
        {
            CloudEnvironmentRepository = Requires.NotNull(cloudEnvironmentRepository, nameof(cloudEnvironmentRepository));
        }

        /// <summary>
        /// Gets the env repository.
        /// </summary>
        protected ICloudEnvironmentRepository CloudEnvironmentRepository { get; }

        /// <inheritdoc/>
        protected override string EntityIdProperty => EnvironmentLoggingPropertyConstants.EnvironmentId;

        /// <inheritdoc/>
        protected override Task<bool> UpdateRecordAsync(
            TPayload payload,
            IEntityRecordRef<CloudEnvironment> record,
            Func<CloudEnvironment, IDiagnosticsLogger, Task<bool>> mutateRecordCallback,
            IDiagnosticsLogger logger)
        {
            return CloudEnvironmentRepository.UpdateRecordAsync(payload.EntityId, record, mutateRecordCallback, logger, LogBaseName);
        }

        /// <inheritdoc/>
        protected override Task<IEntityRecordRef<CloudEnvironment>> FetchReferenceAsync(TPayload payload, IDiagnosticsLogger logger)
        {
            return CloudEnvironmentRepository.FetchReferenceAsync(payload.EntityId, logger);
        }

        /// <summary>
        /// Gets the transition for this opration.
        /// </summary>
        /// <param name="payload">Target input.</param>
        /// <param name="record">Target record that should be deleted.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>Transition state.</returns>
        protected abstract TransitionState FetchOperationTransition(
            TPayload payload,
            IEntityRecordRef<CloudEnvironment> record,
            IDiagnosticsLogger logger);

        /// <inheritdoc/>
        protected override Task<bool> UpdateRecordStatusCallbackAsync(
            TPayload payload,
            IEntityRecordRef<CloudEnvironment> record,
            OperationState state,
            string trigger,
            IDiagnosticsLogger logger)
        {
            // Get transition
            var transition = FetchOperationTransition(payload, record, logger);

            // Update transition
            transition.Reason = payload.Reason;
            var changed = transition.UpdateStatus(state, trigger);

            return Task.FromResult(changed);
        }
    }
}
