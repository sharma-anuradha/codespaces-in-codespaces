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
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
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
            var resources = new ResourcePair(ResourceRepository);

            // Fetch Resource
            await resources.PopulateAwait(input.ComputeResourceId, input.StorageResourceId, logger);

            // First time through, queue things up
            if (status == null)
            {
                // Add record to database
                result = await QueueStartRequestAsync(input, resources, logger);
            }
            else
            {
                // If this is our real first time through, build up the input
                if (computeProviderStartInput == null)
                {
                    // Assign storage
                    var storageResult = await AssignStorageAsync(resources, logger);

                    // Build the compute start input
                    computeProviderStartInput = BuildComputeInputAsync(input, resources.Compute.AzureResourceInfo, storageResult, logger);
                }

                // Deal with error case
                if (computeProviderStartInput != null)
                {
                    // Start the compute
                    var computeResult = await StartComputeAsync(computeProviderStartInput, continuationToken, resources, logger);

                    // Build resource
                    result = new ContinuationResult
                        {
                            Status = computeResult.Status,
                            ContinuationToken = computeResult.ContinuationToken,
                            RetryAfter = computeResult.RetryAfter,
                            NextInput = input,
                        };
                }
                else
                {
                    // Update compute to deal with the fact that storage has bombed
                    await resources.UpdateComputeStartStatus(OperationState.Failed, logger);

                    // Setup failed result
                    result = new ContinuationResult { Status = OperationState.Failed };
                }
            }

            return new ContinuationTaskMessageHandlerResult
                {
                    Result = result,
                    Metadata = computeProviderStartInput,
                };
        }

        private async Task<ContinuationResult> QueueStartRequestAsync(StartEnvironementContinuationInput input, ResourcePair resources, IDiagnosticsLogger logger)
        {
            var nextStatus = OperationState.Initialized;

            // Update status
            await resources.UpdateComputeStartStatus(nextStatus, logger);
            await resources.UpdateStorageStartStatus(nextStatus, logger);

            // Build resource
            return new ContinuationResult
                {
                    Status = nextStatus,
                    ContinuationToken = input.ComputeResourceId,
                    RetryAfter = TimeSpan.Zero,
                    NextInput = input,
                };
        }

        private VirtualMachineProviderStartComputeInput BuildComputeInputAsync(
            StartEnvironementContinuationInput input,
            AzureResourceInfo computeAzureResourceInfo,
            FileShareProviderAssignResult storageResult,
            IDiagnosticsLogger logger)
        {
            // Start compute preperation process
            var computeStorageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageResult);

            return new VirtualMachineProviderStartComputeInput(
                Guid.Parse(input.ComputeResourceId), // TODO: should Ids be Guid or string consistently throughout?
                computeAzureResourceInfo,
                computeStorageFileShareInfo,
                input.EnvironmentVariables);
        }

        private async Task<FileShareProviderAssignResult> AssignStorageAsync(ResourcePair resources, IDiagnosticsLogger logger)
        {
            // Update storage to be inprogress
            await resources.UpdateStorageStartStatus(OperationState.InProgress, logger);

            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = resources.Storage.AzureResourceInfo,
            };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await resources.UpdateStorageStartStatus(storageResult.Status, logger);

            return storageResult;
        }

        private async Task<VirtualMachineProviderStartComputeResult> StartComputeAsync(
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            string continuationToken,
            ResourcePair resources,
            IDiagnosticsLogger logger)
        {
            // First time through the continuationToken shouldn't be our initial queue continuation
            continuationToken = resources.Compute.StartingStatus == OperationState.Initialized ? null : continuationToken;

            // Only need to update things if we are in init state
            if (resources.Compute.StartingStatus == OperationState.Initialized)
            {
                await resources.UpdateComputeStartStatus(OperationState.InProgress, logger);
            }

            // Start compute command
            var computeResult = await ComputeProvider.StartComputeAsync(computeProviderStartInput, continuationToken);

            // Update status to reflect compute result
            await resources.UpdateComputeStartStatus(computeResult.Status, logger);

            return computeResult;
        }

        // TODO: Might get bought abstracted to be more generalizable in the future
        private class ResourcePair
        {
            public ResourcePair(IResourceRepository resourceRepository)
            {
                ResourceRepository = resourceRepository;
            }

            private IResourceRepository ResourceRepository { get; }

            public ResourceRecord Compute { get; private set; }

            public ResourceRecord Storage { get; private set; }

            public async Task PopulateAwait(string computeId, string storageId, IDiagnosticsLogger logger)
            {
                // Get the resource so that we can update the status
                Compute = await ResourceRepository.GetAsync(computeId, logger.FromExisting());
                if (Compute == null)
                {
                    throw new ResourceNotFoundException(Guid.Parse(computeId));
                }

                Storage = await ResourceRepository.GetAsync(storageId, logger.FromExisting());
                if (Storage == null)
                {
                    throw new ResourceNotFoundException(Guid.Parse(storageId));
                }
            }

            public async Task UpdateComputeStartStatus(OperationState state, IDiagnosticsLogger logger)
            {
                if (Compute.UpdateStartingStatus(state))
                {
                    Compute = await ResourceRepository.UpdateAsync(Compute, logger.FromExisting());
                }
            }

            public async Task UpdateStorageStartStatus(OperationState state, IDiagnosticsLogger logger)
            {
                if (Storage.UpdateStartingStatus(state))
                {
                    Storage = await ResourceRepository.UpdateAsync(Storage, logger.FromExisting());
                }
            }
        }
    }
}
