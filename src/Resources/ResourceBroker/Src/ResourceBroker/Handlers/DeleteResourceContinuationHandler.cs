// <copyright file="DeleteResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

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
        /// <param name="requestQueueProvider">Resource request queue provider.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="keyVaultProvider">KeyVault provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="diskProvider">Disk provider.</param>
        /// <param name="serviceProvider">Service Provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        public DeleteResourceContinuationHandler(
            IResourceRequestQueueProvider requestQueueProvider,
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IDiskProvider diskProvider,
            IKeyVaultProvider keyVaultProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IResourceStateManager resourceStateManager)
            : base(serviceProvider, resourceRepository, resourceStateManager)
        {
            RequestQueueProvider = requestQueueProvider;
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            DiskProvider = diskProvider;
            KeyVaultProvider = keyVaultProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerDelete;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Deleting;

        private IResourceRequestQueueProvider RequestQueueProvider { get; }

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        private IDiskProvider DiskProvider { get; set; }

        private IKeyVaultProvider KeyVaultProvider { get; }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> InitiallyQueueContinuationAsync(
            DeleteResourceContinuationInput input,
            ResourceRecordRef record,
            IDiagnosticsLogger logger)
        {
            // Increment the delete count
            record.Value.DeleteAttemptCount++;

            return base.InitiallyQueueContinuationAsync(input, record, logger);
        }

        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(DeleteResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var operationInput = default(ContinuationInput);
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
                    CustomComponents = resource.Value.Components?.Items?.Values.ToList(),
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
            else if (resource.Value.Type == ResourceType.StorageArchive)
            {
                var blobStorageDetails = resource.Value.GetStorageDetails();

                operationInput = new FileShareProviderDeleteBlobInput
                {
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                    BlobName = blobStorageDetails.ArchiveStorageBlobName,
                    BlobContainerName = blobStorageDetails.ArchiveStorageBlobContainerName,
                };
            }
            else if (resource.Value.Type == ResourceType.OSDisk)
            {
                var queueComponent = resource.Value.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
                operationInput = new DiskProviderDeleteInput
                {
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                    QueueResourceInfo = queueComponent?.AzureResourceInfo,
                };
            }
            else if (resource.Value.Type == ResourceType.KeyVault)
            {
                var didParseLocation = Enum.TryParse(resource.Value.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{resource.Value.Location}' is not supported.");
                }

                operationInput = new KeyVaultProviderDeleteInput
                {
                    AzureLocation = azureLocation,
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                };
            }
            else if (resource.Value.Type == ResourceType.PoolQueue)
            {
                var didParseLocation = Enum.TryParse(resource.Value.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{resource.Value.Location}' is not supported.");
                }

                operationInput = new QueueProviderDeleteInput
                {
                    Location = azureLocation,
                    AzureResourceInfo = resource.Value.AzureResourceInfo,
                    QueueName = resource.Value.AzureResourceInfo.Name,
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

            // Run delete operation if we have something to do
            if (result == null)
            {
                if (resource.Value.Type == ResourceType.ComputeVM)
                {
                    result = await ComputeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)input.OperationInput, logger.NewChildLogger());
                }
                else if (resource.Value.Type == ResourceType.StorageFileShare
                    || resource.Value.Type == ResourceType.StorageArchive)
                {
                    result = await StorageProvider.DeleteAsync((FileShareProviderDeleteInput)input.OperationInput, logger.NewChildLogger());
                }
                else if (resource.Value.Type == ResourceType.OSDisk)
                {
                    result = await DiskProvider.DeleteDiskAsync((DiskProviderDeleteInput)input.OperationInput, logger.NewChildLogger());
                }
                else if (resource.Value.Type == ResourceType.KeyVault)
                {
                    result = await KeyVaultProvider.DeleteAsync((KeyVaultProviderDeleteInput)input.OperationInput, logger.NewChildLogger());
                }
                else if (resource.Value.Type == ResourceType.PoolQueue)
                {
                    result = await RequestQueueProvider.DeletePoolQueueAsync((QueueProviderDeleteInput)input.OperationInput, logger.NewChildLogger());
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

            // Since we don't have the azure resource, we are just going to delete this record
            return await ResourceRepository.DeleteAsync(id, logger.NewChildLogger());
        }
    }
}
