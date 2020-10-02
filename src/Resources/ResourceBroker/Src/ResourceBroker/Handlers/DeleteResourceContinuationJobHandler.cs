// <copyright file="DeleteResourceContinuationJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Handlers;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.QueueProvider.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation job handler that manages delete of a resource
    /// </summary>
    public class DeleteResourceContinuationJobHandler
        : ResourceContinuationJobHandlerBase<DeleteResourceContinuationJobHandler.Payload, EmptyContinuationState, DeleteResourceContinuationJobHandler.DeleteResourceContinuationResult>
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueId = "jobhandler-delete-resource";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="requestQueueProvider">Resource request queue provider.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="keyVaultProvider">KeyVault provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="diskProvider">Disk provider.</param>
        /// <param name="resourceStateManager">Request state Manager to update resource state.</param>
        /// <param name="jobQueueProducerFactory">A job queue producer factory.</param>
        public DeleteResourceContinuationJobHandler(
            IResourceRequestQueueProvider requestQueueProvider,
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IDiskProvider diskProvider,
            IKeyVaultProvider keyVaultProvider,
            IResourceRepository resourceRepository,
            IResourceStateManager resourceStateManager,
            IJobQueueProducerFactory jobQueueProducerFactory)
            : base(resourceRepository, resourceStateManager, jobQueueProducerFactory)
        {
            RequestQueueProvider = requestQueueProvider;
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            DiskProvider = diskProvider;
            KeyVaultProvider = keyVaultProvider;
        }

        /// <inheritdoc/>
        public override string QueueId => DefaultQueueId;

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerDelete;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Deleting;

        private IResourceRequestQueueProvider RequestQueueProvider { get; }

        private IComputeProvider ComputeProvider { get; set; }

        private IStorageProvider StorageProvider { get; set; }

        private IDiskProvider DiskProvider { get; set; }

        private IKeyVaultProvider KeyVaultProvider { get; }

        /// <inheritdoc/>
        protected override Task<ContinuationJobResult<EmptyContinuationState, DeleteResourceContinuationResult>> InitializePayload(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger)
        {
            // Increment the delete count
            record.Value.DeleteAttemptCount++;

            return base.InitializePayload(payload, record, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<EmptyContinuationState, DeleteResourceContinuationResult>> ContinueAsync(Payload payload, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var result = (ContinuationResult)null;
            logger.FluentAddValue("HandlerResourceHasAzureResourceInfo", record.Value.AzureResourceInfo != null);

            if (payload.DeleteInput == null)
            {
                if (record.Value.Type == ResourceType.ComputeVM)
                {
                    var didParseLocation = Enum.TryParse(record.Value.Location, true, out AzureLocation azureLocation);
                    if (!didParseLocation)
                    {
                        throw new NotSupportedException($"Provided location of '{record.Value.Location}' is not supported.");
                    }

                    payload.DeleteInput = new VirtualMachineProviderDeleteInput
                    {
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                        CustomComponents = record.Value.Components?.Items?.Values.ToList(),
                        AzureVmLocation = azureLocation,
                        ComputeOS = record.Value.PoolReference.GetComputeOS(),
                    };
                }
                else if (record.Value.Type == ResourceType.StorageFileShare)
                {
                    payload.DeleteInput = new FileShareProviderDeleteInput
                    {
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                    };
                }
                else if (record.Value.Type == ResourceType.StorageArchive)
                {
                    var blobStorageDetails = record.Value.GetStorageDetails();

                    payload.DeleteInput = new FileShareProviderDeleteBlobInput
                    {
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                        BlobName = blobStorageDetails.ArchiveStorageBlobName,
                        BlobContainerName = blobStorageDetails.ArchiveStorageBlobContainerName,
                    };
                }
                else if (record.Value.Type == ResourceType.OSDisk)
                {
                    var queueComponent = record.Value.Components?.Items?.SingleOrDefault(x => x.Value.ComponentType == ResourceType.InputQueue).Value;
                    payload.DeleteInput = new DiskProviderDeleteInput
                    {
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                        QueueResourceInfo = queueComponent?.AzureResourceInfo,
                    };
                }
                else if (record.Value.Type == ResourceType.KeyVault)
                {
                    var didParseLocation = Enum.TryParse(record.Value.Location, true, out AzureLocation azureLocation);
                    if (!didParseLocation)
                    {
                        throw new NotSupportedException($"Provided location of '{record.Value.Location}' is not supported.");
                    }

                    payload.DeleteInput = new KeyVaultProviderDeleteInput
                    {
                        AzureLocation = azureLocation,
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                    };
                }
                else if (record.Value.Type == ResourceType.PoolQueue)
                {
                    var didParseLocation = Enum.TryParse(record.Value.Location, true, out AzureLocation azureLocation);
                    if (!didParseLocation)
                    {
                        throw new NotSupportedException($"Provided location of '{record.Value.Location}' is not supported.");
                    }

                    payload.DeleteInput = new QueueProviderDeleteInput
                    {
                        Location = azureLocation,
                        AzureResourceInfo = record.Value.AzureResourceInfo,
                        QueueName = record.Value.AzureResourceInfo.Name,
                    };
                }
                else
                {
                    throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
                }
            }

            // Return success if AzureResourceInfo is null as it means there is nothing to do
            if (record.Value.AzureResourceInfo == null)
            {
                result = new ContinuationResult() { Status = OperationState.Succeeded };
            }
            else if (record.Value.Type == ResourceType.ComputeVM)
            {
                result = await ComputeProvider.DeleteAsync((VirtualMachineProviderDeleteInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else if (record.Value.Type == ResourceType.StorageFileShare)
            {
                var operationInput = new FileShareProviderDeleteInput
                {
                    AzureResourceInfo = record.Value.AzureResourceInfo,
                };
                result = await StorageProvider.DeleteAsync((FileShareProviderDeleteInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else if (record.Value.Type == ResourceType.StorageArchive)
            {
                result = await StorageProvider.DeleteAsync((FileShareProviderDeleteBlobInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else if (record.Value.Type == ResourceType.OSDisk)
            {
                result = await DiskProvider.DeleteDiskAsync((DiskProviderDeleteInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else if (record.Value.Type == ResourceType.KeyVault)
            {
                result = await KeyVaultProvider.DeleteAsync((KeyVaultProviderDeleteInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else if (record.Value.Type == ResourceType.PoolQueue)
            {
                result = await RequestQueueProvider.DeletePoolQueueAsync((QueueProviderDeleteInput)payload.DeleteInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {record.Value.Type}");
            }

            // Note: refresh the delete input in case some of the providers change the continuation token
            if (result.NextInput != null)
            {
                payload.DeleteInput = result.NextInput;
            }

            return ToContinuationInfo(result, payload);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationJobResult<EmptyContinuationState, DeleteResourceContinuationResult>> ContinueAsync(IJob<Payload> job, IEntityRecordRef<ResourceRecord> record, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            var result = await base.ContinueAsync(job, record, logger, cancellationToken);
            if (result.ResultState == ContinuationJobPayloadResultState.Succeeded)
            {
                var deleted = await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_delete_record",
                    (childLogger) => DeleteResourceAsync(job.Payload.EntityId.ToString(), childLogger));

                if (!deleted)
                {
                    return ToContinuationInfo(await FailOperationAsync(job.Payload, record, "HandlerResourceRepositoryDeleteFailed", logger), job.Payload);
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

        [JobPayload(JobPayloadNameOption.Name)]
        public class Payload : EntityContinuationJobPayloadBase<EmptyContinuationState>
        {
            /// <summary>
            /// Gets or sets the Environment Id.
            /// </summary>
            public Guid? EnvironmentId { get; set; }

            [JsonConverter(typeof(DeleteInputConverter))]
            public ContinuationInput DeleteInput { get; set; }
        }

        /// <summary>
        /// Json converter for DeleteInput type
        /// </summary>
        public class DeleteInputConverter : JsonTypeConverter
        {
            private static readonly Dictionary<string, Type> MapTypes
                    = new Dictionary<string, Type>
                {
                    { "computeVM", typeof(VirtualMachineProviderDeleteInput) },
                    { "storageFileShare", typeof(FileShareProviderDeleteInput) },
                    { "storageArchive", typeof(FileShareProviderDeleteBlobInput) },
                    { "osDisk", typeof(DiskProviderDeleteInput) },
                    { "keyVault", typeof(KeyVaultProviderDeleteInput) },
                    { "poolQueue", typeof(QueueProviderDeleteInput) },
                };

            protected override Type BaseType => typeof(ContinuationInput);

            protected override IDictionary<string, Type> SupportedTypes => MapTypes;
        }

        public class DeleteResourceContinuationResult : EntityContinuationResult
        {
        }
    }
}
