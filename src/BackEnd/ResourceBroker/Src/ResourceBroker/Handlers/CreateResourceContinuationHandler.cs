// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
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
        /// <param name="subscriptionCatalog">Subscription Catalog to be used.</param>
        /// <param name="blobStorageClientProvider">Blob storage client provider.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="skuCatalog">The environment SKU catalog.</param>
        /// <param name="resourceBrokerSettings">Resource broker settings.</param>
        /// <param name="serviceCollection">Service Collection.</param>
        public CreateResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IResourceRepository resourceRepository,
            IBlobStorageClientProvider blobStorageClientProvider,
            ICapacityManager capacityManager,
            ISkuCatalog skuCatalog,
            ResourceBrokerSettings resourceBrokerSettings,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            BlobStorageClientProvider = blobStorageClientProvider;
            CapacityManager = capacityManager;
            SkuCatalog = skuCatalog;
            ResourceBrokerSettings = resourceBrokerSettings;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        private ICapacityManager CapacityManager { get; }

        private ISkuCatalog SkuCatalog { get; }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        /// <inheritdoc/>
        protected override async Task<ResourceRecordRef> ObtainReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // If we have a reference use that
            if (string.IsNullOrEmpty(input.ContinuationToken))
            {
                return await CreateReferenceAsync(input, logger);
            }

            return await FetchReferenceAsync(input.ResourceId, logger);
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationInput> BuildOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            var result = default(ContinuationInput);

            // Get the resource location from the capacity manager.
            if (!Enum.TryParse<AzureLocation>(input.Location, ignoreCase: true, out var azureLocation))
            {
                throw new NotSupportedException($"Provided location of '{input.Location}' is not supported.");
            }

            /*
             * TODO: How will we track the Cloud Environment SKU in the pool manager, and not just the Azure SKU?
            if (!SkuCatalog.CloudEnvironmentSkus.TryGetValue(input.SkuName, out var sku))
            {
                throw new SkuNotAvailableException(input.SkuName, azureLocation);
            }
            */
            var sku = default(ICloudEnvironmentSku);
            var resourceLocation = await CapacityManager.SelectAzureResourceLocation(sku, azureLocation, logger);
            var resourceGroup = resourceLocation.ResourceGroup;
            var subscription = resourceLocation.Subscription.SubscriptionId;

            if (resource.Value.Type == ResourceType.ComputeVM)
            {
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

            return result;
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

        private async Task<ResourceRecordRef> CreateReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // Common properties
            var id = input.ResourceId;
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
    }
}
