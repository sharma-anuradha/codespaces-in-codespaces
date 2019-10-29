// <copyright file="DeleteResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages starting of environment.
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
        /// <param name="serviceProvider">Service Provider.</param>
        public DeleteResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerDelete;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Deleting;

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> QueueOperationAsync(
            DeleteResourceContinuationInput input,
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Increment the delete count
            record.Value.DeleteAttemptCount++;

            return base.QueueOperationAsync(input, record, logger);
        }

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(DeleteResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var operationInput = (ContinuationInput)null;
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                var didParseLocation = Enum.TryParse(resource.Value.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{resource.Value.Location}' is not supported.");
                }

                operationInput = new VirtualMachineProviderDeleteInput
                {
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                    AzureVmLocation = azureLocation,
                    ComputeOS = resource.Value.PoolReference.GetComputeOS(),
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
                throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
            }

            return Task.FromResult(operationInput);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(DeleteResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var result = (ContinuationResult)null;

            logger.FluentAddValue("HandlerResourceHasAzureResourceInfo", resource.Value.AzureResourceInfo != null);

            // Return success if AzureResourceInfo is null as it means there is nothing to do
            if (resource.Value.AzureResourceInfo == null)
            {
                result = new ContinuationResult() { Status = OperationState.Succeeded };
            }

            // Run create operation if we have something to do
            if (result == null)
            {
                if (resource.Value.Type == ResourceType.ComputeVM)
                {
                    result = await ComputeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)input.OperationInput, logger.WithValues(new LogValueSet()));
                }
                else if (resource.Value.Type == ResourceType.StorageFileShare)
                {
                    result = await StorageProvider.DeleteAsync((FileShareProviderDeleteInput)input.OperationInput, logger.WithValues(new LogValueSet()));
                }
                else
                {
                    throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
                }
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
                var deleted = await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_delete_record",
                    (childLogger) => DeleteResourceAsync(input.ResourceId.ToString(), childLogger));

                if (!deleted)
                {
                    return await FailOperationAsync(input, record, "HandlerResourceRepositoryDeleteFailed", logger);
                }
            }

            return result;
        }

        private async Task<bool> DeleteResourceAsync(string id, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue(ResourceLoggingPropertyConstants.ResourceId, id)
                .FluentAddBaseValue(ResourceLoggingPropertyConstants.OperationReason, "DeleteResourceContinuation");

            // Since we don't have the azyre resource, we are just goignt to delete this record
            return await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }
    }
}
