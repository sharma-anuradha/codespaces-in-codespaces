// <copyright file="DeleteResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environement.
    /// </summary>
    public class DeleteResourceContinuationHandler
        : BaseContinuationTaskMessageHandler<DeleteResourceContinuationInput>, IDeleteResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobDeleteResource";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        public DeleteResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository)
            : base(resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
        }

        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Deleting;

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(DeleteResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var operationInput = (ContinuationInput)null;
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                operationInput = new VirtualMachineProviderDeleteInput
                {
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                };
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                operationInput = new FileShareProviderDeleteInput
                {
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                };
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return Task.FromResult(operationInput);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationAsync(ContinuationInput operationInput, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var result = (ContinuationResult)null;

            // Run create operation
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                result = await ComputeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)operationInput, logger.WithValues(new LogValueSet()));
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                result = await StorageProvider.DeleteAsync((FileShareProviderDeleteInput)operationInput, logger.WithValues(new LogValueSet()));
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return result;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationAsync(DeleteResourceContinuationInput input, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            // Perform core operation first
            var result = await base.RunOperationAsync(input, record, logger);

            // Make sure we bring over the Resource info if we have it
            if (result.Status == OperationState.Succeeded)
            {
                var deleted = await ResourceRepository.DeleteAsync(input.ResourceId.ToString(), logger.WithValues(new LogValueSet()));
                if (!deleted)
                {
                    return await FailOperationAsync(input, record, logger);
                }
            }

            return result;
        }
    }
}
