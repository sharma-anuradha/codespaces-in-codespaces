// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// 
    /// </summary>
    public class DeleteResourceContinuationHandler : IDeleteResourceContinuationHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider"></param>
        /// <param name="storageProvider"></param>
        /// <param name="resourceRepository"></param>
        /// <param name="subscriptionCatalog"></param>
        /// <param name="mapper"></param>
        public DeleteResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IAzureSubscriptionCatalog subscriptionCatalog,
            IMapper mapper)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            ResourceRepository = resourceRepository;
            SubscriptionCatalog = subscriptionCatalog;
            Mapper = mapper;
        }

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IResourceRepository ResourceRepository { get; }

        private IAzureSubscriptionCatalog SubscriptionCatalog { get; }

        private IMapper Mapper { get; }

        /// <inheritdoc/>
        public virtual bool CanHandle(ResourceJobQueuePayload payload)
        {
            return payload.Target == "JobDeleteResource";
        }

        /// <inheritdoc/>
        public async Task<ContinuationTaskMessageHandlerResult> Continue(
            ContinuationTaskMessageHandlerInput handlerInput,
            IDiagnosticsLogger logger)
        {
            var input = Mapper.Map<DeleteResourceContinuationInput>(handlerInput.Input);

            return await Continue(input, handlerInput.Status, handlerInput.ContinuationToken, logger);
        }

        private async Task<ContinuationTaskMessageHandlerResult> Continue(
            DeleteResourceContinuationInput input,
            OperationState? status,
            string continuationToken,
            IDiagnosticsLogger logger)
        {
            var result = (ContinuationResult)null;
            var resourceReference = new ResourceReference(ResourceRepository);

            // Fetch Resource
            await resourceReference.PopulateAsync(input.ResourceId, logger);

            // First time through, queue things up
            if (status == null)
            {
                // Add record to database
                result = await QueueDeleteRequestAsync(input, resourceReference, logger);
            }
            else
            {
                // Delete the resource
                result = await DeleteResourceAsync(input, continuationToken, resourceReference, logger);
            }

            return new ContinuationTaskMessageHandlerResult { Result = result };
        }

        private async Task<ContinuationResult> QueueDeleteRequestAsync(
            DeleteResourceContinuationInput input,
            ResourceReference reference,
            IDiagnosticsLogger logger)
        {
            // Flag necessary properties
            reference.Resource.IsReady = false;
            reference.Resource.IsDeleted = true;

            // Update status
            await reference.SaveDeletingStatus(OperationState.Initialized, logger);

            // Build resource
            return new ContinuationResult
            {
                Status = OperationState.Initialized,
                ContinuationToken = input.ResourceId.ToString(),
                RetryAfter = TimeSpan.Zero,
                NextInput = input,
            };
        }

        private async Task<ContinuationResult> DeleteResourceAsync(
            DeleteResourceContinuationInput input,
            string continuationToken,
            ResourceReference reference,
            IDiagnosticsLogger logger)
        {
            // First time through the continuationToken shouldn't be our initial queue continuation
            continuationToken = reference.Resource.DeletingStatus == OperationState.Initialized ? null : continuationToken;

            // Only need to update things if we are in init state
            if (reference.Resource.DeletingStatus == OperationState.Initialized)
            {
                await reference.SaveDeletingStatus(OperationState.InProgress, logger);
            }

            // Delete compute command
            var deleteResult = (ContinuationResult)null;
            if (reference.Resource.Type == ResourceType.ComputeVM)
            {
                deleteResult = await CoreDeleteComputeAsync(
                    reference.Resource.AzureResourceInfo,
                    continuationToken,
                    logger);
            }
            else if (reference.Resource.Type == ResourceType.StorageFileShare)
            {
                deleteResult = await CoreDeleteStorageAsync(
                    reference.Resource.AzureResourceInfo,
                    continuationToken,
                    logger);
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {reference.Resource.Type}");
            }

            // Update status to reflect compute result
            await reference.SaveDeletingStatus(deleteResult.Status, logger);

            // Delete the docdb record
            var deleteStatus = deleteResult.Status;
            if (reference.Resource.DeletingStatus == OperationState.Succeeded)
            {
                var deleteDbResult = await ResourceRepository.DeleteAsync(input.ResourceId.ToString(), logger);

                // Deal wit the case where detele didn't work
                if (!deleteDbResult)
                {
                    deleteStatus = OperationState.Failed;
                }
            }

            return new ContinuationResult
            {
                Status = deleteStatus,
                ContinuationToken = deleteResult.ContinuationToken,
                RetryAfter = deleteResult.RetryAfter,
                NextInput = input,
            };
        }

        protected async Task<ContinuationResult> CoreDeleteComputeAsync(AzureResourceInfo azureResourceInfo, string continuationToken, IDiagnosticsLogger logger)
        {
            var providerInput = new VirtualMachineProviderDeleteInput
            {
                AzureResourceInfo = azureResourceInfo,
            };

            return await ComputeProvider.DeleteAsync(providerInput, logger, continuationToken);
        }


        protected async Task<ContinuationResult> CoreDeleteStorageAsync(AzureResourceInfo azureResourceInfo, string continuationToken, IDiagnosticsLogger logger)
        {
            var providerInput = new FileShareProviderDeleteInput
            {
                AzureResourceInfo = azureResourceInfo,
            };

            return await StorageProvider.DeleteAsync(providerInput, logger, continuationToken);
        }
    }
}
