// <copyright file="DeleteResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages creating of environement.
    /// </summary>
    public class CreateResourceContinuationHandler
        : BaseContinuationTaskMessageHandler<CreateResourceContinuationInput>, ICreateResourceContinuationHandler
    {
        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        public const string DefaultQueueTarget = "JobCreateResource";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceContinuationHandler"/> class.
        /// </summary>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="blobStorageClientProvider">Blob storage client provider.</param>
        /// <param name="resourceBrokerSettings">Resource broker settings.</param>
        public CreateResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IAzureSubscriptionCatalog subscriptionCatalog,
            IBlobStorageClientProvider blobStorageClientProvider,
            ResourceBrokerSettings resourceBrokerSettings)
            : base(resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            SubscriptionCatalog = subscriptionCatalog;
            BlobStorageClientProvider = blobStorageClientProvider;
            ResourceBrokerSettings = resourceBrokerSettings;
        }

        /// <summary>
        /// Gets default target name for item on queue.
        /// </summary>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IAzureSubscriptionCatalog SubscriptionCatalog { get; }

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        protected override async Task<ResourceRecordRef> ObtainReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // If we have a reference use that
            if (input.ResourceId.HasValue)
            {
                return await base.FetchReferenceAsync(input.ResourceId.Value, logger);
            }

            return await CreateReferenceAsync(input, logger);
        }

        private async Task<ResourceRecordRef> CreateReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // Common properties
            var id = Guid.NewGuid();
            var time = DateTime.UtcNow;

            // Core recrod
            var record = new ResourceRecord
            {
                Id = id.ToString(),
                Type = input.Type,
                IsReady = false,
                Ready = null,
                IsAssigned = false,
                Assigned = null,
                Created = time,
                Location = input.Location,
                SkuName = input.SkuName,
            };

            // Update input
            input.ResourceId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }


        /// <inheritdoc/>
        protected override Task<ContinuationInput> BuildOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var result = (ContinuationInput)null;

            // TODO: Update to get info from the capacity manager
            var resourceGroup = $"RG-{input.ResourceId}";
            var subscription = SubscriptionCatalog.AzureSubscriptions.FirstOrDefault().SubscriptionId;

            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                var didParseLocation = Enum.TryParse(input.Location, true, out AzureLocation azureLocation);
                if (!didParseLocation)
                {
                    throw new NotSupportedException($"Provided location of '{input.Location}' is not supported.");
                }

                result = new VirtualMachineProviderCreateInput
                {
                    AzureVmLocation = azureLocation,
                    AzureSkuName = input.SkuName,
                    AzureSubscription = Guid.Parse(subscription),
                    AzureResourceGroup = resourceGroup,
                    AzureVirtualMachineImage = "Canonical:UbuntuServer:18.04-LTS:latest",
                };
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                var container = BlobStorageClientProvider.GetCloudBlobContainer(ResourceBrokerSettings.FileShareTemplateContainerName);
                var blob = container.GetBlobReference(ResourceBrokerSettings.FileShareTemplateBlobName);
                var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4), // This should be plenty of time to copy the blob template into the new file share
                });

                result = new FileShareProviderCreateInput
                {
                    AzureLocation = input.Location,
                    AzureSkuName = input.SkuName,
                    AzureSubscription = subscription,
                    AzureResourceGroup = resourceGroup,
                    StorageBlobUrl = blob.Uri + sas,
                };
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationAsync(ContinuationInput operationInput, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var result = (ResourceCreateContinuationResult)null;

            // Run create operation
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                result = await ComputeProvider.CreateAsync((VirtualMachineProviderCreateInput)operationInput, logger.WithValues(new LogValueSet()));
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                result = await StorageProvider.CreateAsync((FileShareProviderCreateInput)operationInput, logger.WithValues(new LogValueSet()));
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            // Make sure we bring over the Resource info if we have it
            if (resource.Value.AzureResourceInfo == null && result.AzureResourceInfo != null)
            {
                resource.Value.AzureResourceInfo = result.AzureResourceInfo;

                resource.Value = await ResourceRepository.UpdateAsync(resource.Value, logger.WithValues(new LogValueSet()));
            }

            return result;
        }
    }
}
