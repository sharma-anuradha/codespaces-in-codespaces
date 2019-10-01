// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
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
        /// <param name="resourcePoolManager">Target resource pool manager.</param>
        /// <param name="computeProvider">Target compute provider.</param>
        /// <param name="storageProvider">Target storatge provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Target control plane resource accessor.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">the control plane resource accessor.</param>
        /// <param name="controlPlaneInfo">the control plane info.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="resourceBrokerSettings">Resource broker settings.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="virtualMachineTokenProvider">Virtual machine token provider.</param>
        public CreateResourceContinuationHandler(
            IResourcePoolManager resourcePoolManager,
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ICapacityManager capacityManager,
            ResourceBrokerSettings resourceBrokerSettings,
            IVirtualMachineTokenProvider virtualMachineTokenProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider)
            : base(serviceProvider, resourceRepository)
        {
            ResourcePoolManager = resourcePoolManager;
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            ControlPlaneInfo = controlPlaneInfo;
            CapacityManager = capacityManager;
            ResourceBrokerSettings = resourceBrokerSettings;
            VirtualMachineTokenProvider = virtualMachineTokenProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        private IResourcePoolManager ResourcePoolManager { get; }

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }
        
        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICapacityManager CapacityManager { get; }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private IVirtualMachineTokenProvider VirtualMachineTokenProvider { get; }

        /// <inheritdoc/>
        protected override Task<ContinuationResult> QueueOperationAsync(CreateResourceContinuationInput input, ResourceRecordRef record, IDiagnosticsLogger logger)
        {
            // Determine if the pool is currently enabled
            var poolEnabled = ResourcePoolManager.IsPoolEnabled(input.ResourcePoolDetails.GetPoolDefinition());

            logger.FluentAddValue("HandlerIsPoolEnabled", poolEnabled);

            // Short circuit things if we have a fail
            if (!poolEnabled)
            {
                return Task.FromResult(new ContinuationResult() { Status = OperationState.Cancelled });
            }

            return base.QueueOperationAsync(input, record, logger);
        }

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

            var resourceTags = new Dictionary<string, string>()
            {
                { ResourceTagName.ResourceId, resource.Value.Id },
                { ResourceTagName.ResourceType, resource.Value.Type.ToString() },
            };

            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                // Ensure that the details type is correct
                if (input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails)
                {
                    // Set up the selection criteria and select a subscription/location.
                    var criteria = new List<AzureResourceCriterion>
                    {
                        // SkuFamily must be first as the primary criterion for ordering candidate subscriptions.
                        new AzureResourceCriterion { ServiceType = ServiceType.Compute, Quota = computeDetails.SkuFamily, Required = computeDetails.Cores },
                        new AzureResourceCriterion { ServiceType = ServiceType.Compute, Quota = "cores", Required = computeDetails.Cores },
                        new AzureResourceCriterion { ServiceType = ServiceType.Network, Quota = "VirtualNetworks", Required = 1 },
                    };
                    var resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                        criteria,
                        input.ResourcePoolDetails.Location,
                        logger);
                    var resourceGroup = resourceLocation.ResourceGroup;
                    var subscription = resourceLocation.Subscription.SubscriptionId;

                    var token = await VirtualMachineTokenProvider.GenerateAsync(resource.Value.Id, logger);
                    var blobStorageClientProvider = await GetVmAgentImageBlobStorageClientProvider(input.ResourcePoolDetails.Location);
                    var url = GetBlobUrlWithSasToken(ResourceBrokerSettings.VirtualMachineAgentContainerName, computeDetails.VmAgentImageName, blobStorageClientProvider);

                    resourceTags.Add(ResourceTagName.ComputeOS, computeDetails.OS.ToString());

                    result = new VirtualMachineProviderCreateInput
                    {
                        ResourceId = resource.Value.Id,
                        VMToken = token,
                        AzureVmLocation = computeDetails.Location,
                        AzureSkuName = computeDetails.SkuName,
                        AzureSubscription = Guid.Parse(subscription),
                        AzureResourceGroup = resourceGroup,
                        AzureVirtualMachineImage = computeDetails.ImageName,
                        VmAgentBlobUrl = url,
                        ResourceTags = resourceTags,
                        ComputeOS = computeDetails.OS,
                        FrontDnsHostName = ControlPlaneInfo.Stamp.DnsHostName,
                    };
                }
                else
                {
                    throw new NotSupportedException($"Pool compute details type is not selected - {input.ResourcePoolDetails.GetType()}");
                }
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                // Ensure that the details type is correct
                if (input.ResourcePoolDetails is ResourcePoolStorageDetails storageDetails)
                {
                    // Set up the selection criteria and select a subscription/location.
                    var criteria = new List<AzureResourceCriterion>
                    {
                        new AzureResourceCriterion { ServiceType = ServiceType.Storage, Quota = "StorageAccounts", Required = 1 },
                    };
                    var resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                        criteria,
                        input.ResourcePoolDetails.Location,
                        logger);
                    var resourceGroup = resourceLocation.ResourceGroup;
                    var subscription = resourceLocation.Subscription.SubscriptionId;

                    // Get storage SAS token
                    var blobStorageClientProvider = await GetStorageImageBlobStorageClientProvider(input.ResourcePoolDetails.Location);
                    var url = GetBlobUrlWithSasToken(ResourceBrokerSettings.FileShareTemplateContainerName, storageDetails.ImageName, blobStorageClientProvider);

                    result = new FileShareProviderCreateInput
                    {
                        AzureLocation = storageDetails.Location.ToString().ToLowerInvariant(),
                        AzureSkuName = storageDetails.SkuName,
                        AzureSubscription = subscription,
                        AzureResourceGroup = resourceGroup,
                        StorageBlobUrl = url,
                        ResourceTags = resourceTags,
                    };
                }
                else
                {
                    throw new NotSupportedException($"Pool storage details type is not selected - {input.ResourcePoolDetails.GetType()}");
                }
            }
            else
            {
                throw new NotSupportedException($"Resource type is not supported - {resource.Value.Type}");
            }

            return result;
        }

        private string GetBlobUrlWithSasToken(string containerName, string blobName, IBlobStorageClientProvider blobStorageClientProvider)
        {
            var container = blobStorageClientProvider.GetCloudBlobContainer(containerName);
            var blob = container.GetBlobReference(blobName);
            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4),
            });

            return blob.Uri + sas;
        }

        /// <inheritdoc/>
        protected override async Task<ContinuationResult> RunOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            ResourceCreateContinuationResult result;

            // Run create operation
            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                result = await ComputeProvider.CreateAsync((VirtualMachineProviderCreateInput)input.OperationInput, logger.WithValues(new LogValueSet()));
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                result = await StorageProvider.CreateAsync((FileShareProviderCreateInput)input.OperationInput, logger.WithValues(new LogValueSet()));
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            // Make sure we bring over the Resource info if we have it
            if (resource.Value.AzureResourceInfo == null && result.AzureResourceInfo != null)
            {
                resource.Value.AzureResourceInfo = result.AzureResourceInfo;
                resource.Value.PoolReference.Dimensions = input.ResourcePoolDetails.GetPoolDimensions();

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
                Location = input.ResourcePoolDetails.Location.ToString().ToLowerInvariant(),
                SkuName = input.ResourcePoolDetails.SkuName,
                PoolReference = new ResourcePoolDefinitionRecord
                {
                    Code = input.ResourcePoolDetails.GetPoolDefinition(),
                    VersionCode = input.ResourcePoolDetails.GetPoolVersionDefinition(),
                },
            };

            // Update input
            input.ResourceId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }

        private async Task<IBlobStorageClientProvider> GetStorageImageBlobStorageClientProvider(AzureLocation azureLocation)
        {
            var (blobStorageAccountName, blobStorageAccountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForStorageImagesAsync(azureLocation);
            var blobStorageClientOptions = new BlobStorageClientOptions
            {
                AccountName = blobStorageAccountName,
                AccountKey = blobStorageAccountKey,
            };
            var blobStorageClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
            return blobStorageClientProvider;
        }

        private async Task<IBlobStorageClientProvider> GetVmAgentImageBlobStorageClientProvider(AzureLocation azureLocation)
        {
            var (blobStorageAccountName, blobStorageAccountKey) = await ControlPlaneAzureResourceAccessor.GetStampStorageAccountForComputeVmAgentImagesAsync(azureLocation);
            var blobStorageClientOptions = new BlobStorageClientOptions
            {
                AccountName = blobStorageAccountName,
                AccountKey = blobStorageAccountKey,
            };
            var blobStorageClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
            return blobStorageClientProvider;
        }
    }
}
