// <copyright file="StartEnvironmentContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class StartEnvironmentContinuationHandler : IStartEnvironmentContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartEnvironmentContinuationHandler"/> class.
        /// </summary>
        /// <param name="resourceRepository"></param>
        /// <param name="computeProvider"></param>
        /// <param name="storageProvider"></param>
        /// <param name="mapper"></param>
        public StartEnvironmentContinuationHandler(
            IResourceRepository resourceRepository,
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IMapper mapper)
        {
            ResourceRepository = resourceRepository;
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            Mapper = mapper;
        }

        private IResourceRepository ResourceRepository { get; }

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IMapper Mapper { get; }

        private string TargetName { get; } = "JobStartCompute";

        /// <inheritdoc/>
        public virtual bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == TargetName;
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Continue(
            ContinuationTaskMessageHandlerInput handlerInput,
            IDiagnosticsLogger logger)
        {
            var input = Mapper.Map<StartEnvironementContinuationInput>(handlerInput.Input);
            var computeProviderStartInput = Mapper.Map<VirtualMachineProviderStartComputeInput>(handlerInput.Metadata);

            return await Continue(input, computeProviderStartInput, handlerInput.Status, handlerInput.ContinuationToken, logger);
        }

        /// <inheritdoc/>
        protected virtual async Task<ContinuationTaskMessageHandlerResult> Continue(
            StartEnvironementContinuationInput input,
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            OperationState? status,
            string continuationToken,
            IDiagnosticsLogger logger)
        {
            var result = (ContinuationResult)null;

            // Get the resource so that we can update the status
            var computeRecord = await ResourceRepository.GetByResourceId(input.ComputeResourceId.ToString(), logger.FromExisting());
            if (computeRecord == null)
            {
                throw new ArgumentNullException($"Was not able to find target compute resource - {input.ComputeResourceId}.");
            }

            var storageRecord = await ResourceRepository.GetByResourceId(input.StorageResourceId, logger.FromExisting());
            if (computeRecord == null)
            {
                throw new ArgumentNullException($"Was not able to find target storage resource - {input.StorageResourceId}.");
            }

            if (status == null)
            {
                // Add record to database
                result = await CreateQueueRequestAsync(input, computeRecord, storageRecord, logger);
            }
            else
            {
                // If this is our real first time through, build up the input
                if (status.Value == OperationState.Initialized)
                {
                    computeProviderStartInput = await BuildComputeInputAsync(input, computeRecord, storageRecord, logger);
                }

                // Deal with error case
                if (computeProviderStartInput != null)
                {
                    result = await NextQueueRequestAsync(input, computeProviderStartInput, computeRecord, logger, continuationToken);
                }
                else
                {
                    result = new ContinuationResult { Status = OperationState.Failed };
                }
            }

            return new ContinuationTaskMessageHandlerResult
                {
                    Result = result,
                    Metadata = computeProviderStartInput,
                };
        }

        private async Task<ContinuationResult> CreateQueueRequestAsync(StartEnvironementContinuationInput input, ResourceRecord computeRecord, ResourceRecord storageRecord, IDiagnosticsLogger logger)
        {
            var nextStatus = OperationState.Initialized;

            // Update status
            await UpdateResourceStatus(computeRecord, nextStatus, logger);
            await UpdateResourceStatus(storageRecord, nextStatus, logger);

            // Build resource
            return new ContinuationResult
                {
                    Status = nextStatus,
                    ContinuationToken = input.ComputeResourceId.InstanceId.ToString(),
                    RetryAfter = TimeSpan.Zero,
                    NextInput = input,
                };
        }

        private async Task<ContinuationResult> NextQueueRequestAsync(StartEnvironementContinuationInput input, VirtualMachineProviderStartComputeInput computeProviderStartInput, ResourceRecord record, IDiagnosticsLogger logger, string continuationToken)
        {
            // Make the core udpate
            var computeResult = await ComputeProvider.StartComputeAsync(computeProviderStartInput, continuationToken);

            // Update status
            await UpdateResourceStatus(record, computeResult.Status, logger);

            // Build resource
            return new ContinuationResult
            {
                Status = computeResult.Status,
                ContinuationToken = computeResult.ContinuationToken,
                RetryAfter = computeResult.RetryAfter,
                NextInput = input,
            };
        }

        private async Task<ResourceRecord> UpdateResourceStatus(
            ResourceRecord resource, OperationState state, IDiagnosticsLogger logger)
        {
            // Set status
            resource.UpdateStartingStatus(state);

            return await ResourceRepository.UpdateAsync(resource, logger.FromExisting());
        }

        private async Task<VirtualMachineProviderStartComputeInput> BuildComputeInputAsync(
            StartEnvironementContinuationInput input,
            ResourceRecord computeRecord,
            ResourceRecord storageRecord,
            IDiagnosticsLogger logger)
        {
            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput { ResourceId = input.StorageResourceId };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, null);

            // Update storage to be completed
            await UpdateResourceStatus(storageRecord, storageResult.Status, logger.FromExisting());

            // Deal with the case that its not a success
            if (storageResult.Status != OperationState.Succeeded)
            {
                // Update compute to deal with the fact that storage has bombed
                await UpdateResourceStatus(computeRecord, storageResult.Status, logger.FromExisting());

                return null;
            }

            // Start compute preperation process
            var computeStorageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageResult);
            return new VirtualMachineProviderStartComputeInput(
                input.ComputeResourceId,
                computeStorageFileShareInfo,
                input.EnvironmentVariables);
        }
    }
}
