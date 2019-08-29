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
            var input = Mapper.Map<StartEnvironmentContinuationInput>(handlerInput.Input);
            var computeProviderStartInput = Mapper.Map<VirtualMachineProviderStartComputeInput>(handlerInput.Metadata);

            return await Continue(input, computeProviderStartInput, handlerInput.Status, handlerInput.ContinuationToken, logger);
        }

        /// <inheritdoc/>
        protected virtual async Task<ContinuationTaskMessageHandlerResult> Continue(
            StartEnvironmentContinuationInput input,
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            OperationState? status,
            string continuationToken,
            IDiagnosticsLogger logger)
        {
            var result = (ContinuationResult)null;
            var computeReference = new ResourceReference(ResourceRepository);
            var storageReference = new ResourceReference(ResourceRepository);

            // Fetch Resource
            await computeReference.PopulateAsync(input.ComputeResourceId, logger);
            await storageReference.PopulateAsync(input.StorageResourceId, logger);

            // First time through, queue things up
            if (status == null)
            {
                // Add record to database
                result = await QueueStartRequestAsync(input, computeReference, storageReference, logger);
            }
            else
            {
                // If this is our real first time through, build up the input
                if (computeProviderStartInput == null)
                {
                    // Assign storage
                    var storageResult = await AssignStorageAsync(storageReference, logger);

                    // Build the compute start input
                    computeProviderStartInput = BuildComputeInputAsync(input, computeReference.Resource.AzureResourceInfo, storageResult, logger);
                }

                // Deal with error case
                if (computeProviderStartInput != null)
                {
                    // Start the compute
                    result = await StartComputeAsync(input, computeProviderStartInput, continuationToken, computeReference, logger);
                }
                else
                {
                    // Fail the operation
                    result = await FailComputeAsync(computeReference, logger);
                }
            }

            return new ContinuationTaskMessageHandlerResult
                {
                    Result = result,
                    Metadata = computeProviderStartInput,
                };
        }

        private async Task<ContinuationResult> QueueStartRequestAsync(
            StartEnvironmentContinuationInput input,
            ResourceReference computeReference,
            ResourceReference storageReference,
            IDiagnosticsLogger logger)
        {
            // Update status
            await computeReference.SaveStartingStatus(OperationState.Initialized, logger);
            await storageReference.SaveStartingStatus(OperationState.Initialized, logger);

            // Build resource
            return new ContinuationResult
                {
                    Status = OperationState.Initialized,
                    ContinuationToken = input.ComputeResourceId.ToString(),
                    RetryAfter = TimeSpan.Zero,
                    NextInput = input,
                };
        }

        private VirtualMachineProviderStartComputeInput BuildComputeInputAsync(
            StartEnvironmentContinuationInput input,
            AzureResourceInfo computeAzureResourceInfo,
            FileShareProviderAssignResult storageResult,
            IDiagnosticsLogger logger)
        {
            // Start compute preperation process
            var computeStorageFileShareInfo = Mapper.Map<ShareConnectionInfo>(storageResult);

            return new VirtualMachineProviderStartComputeInput(
                input.ComputeResourceId,
                computeAzureResourceInfo,
                computeStorageFileShareInfo,
                input.EnvironmentVariables);
        }

        private async Task<FileShareProviderAssignResult> AssignStorageAsync(ResourceReference storageReference, IDiagnosticsLogger logger)
        {
            // Update storage to be inprogress
            await storageReference.SaveStartingStatus(OperationState.InProgress, logger);

            // Get file share connection info for target share
            var fileShareProviderAssignInput = new FileShareProviderAssignInput
            {
                AzureResourceInfo = storageReference.Resource.AzureResourceInfo,
            };
            var storageResult = await StorageProvider.AssignAsync(fileShareProviderAssignInput, logger);

            // Update storage to be completed
            await storageReference.SaveStartingStatus(storageResult.Status, logger);

            return storageResult;
        }

        private async Task<ContinuationResult> StartComputeAsync(
            StartEnvironmentContinuationInput input,
            VirtualMachineProviderStartComputeInput computeProviderStartInput,
            string continuationToken,
            ResourceReference computeReference,
            IDiagnosticsLogger logger)
        {
            // First time through the continuationToken shouldn't be our initial queue continuation
            continuationToken = computeReference.Resource.StartingStatus == OperationState.Initialized ? null : continuationToken;

            // Only need to update things if we are in init state
            if (computeReference.Resource.StartingStatus == OperationState.Initialized)
            {
                await computeReference.SaveStartingStatus(OperationState.InProgress, logger);
            }

            // Start compute command
            var computeResult = await ComputeProvider.StartComputeAsync(computeProviderStartInput, continuationToken);

            // Update status to reflect compute result
            await computeReference.SaveStartingStatus(computeResult.Status, logger);

            // Build resource
            return new ContinuationResult
            {
                Status = computeResult.Status,
                ContinuationToken = computeResult.ContinuationToken,
                RetryAfter = computeResult.RetryAfter,
                NextInput = input,
            };
        }

        private async Task<ContinuationResult> FailComputeAsync(
            ResourceReference computeReference,
            IDiagnosticsLogger logger)
        {
            // Update compute to deal with the fact that storage has bombed
            await computeReference.SaveStartingStatus(OperationState.Failed, logger);

            // Setup failed result
            return new ContinuationResult { Status = OperationState.Failed };
        }
    }
}
