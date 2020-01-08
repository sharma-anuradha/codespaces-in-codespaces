// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Continuation handler that manages creating of environment.
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
        /// <param name="computeProvider">Target compute provider.</param>
        /// <param name="storageProvider">Target storatge provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Target control plane resource accessor.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">the control plane resource accessor.</param>
        /// <param name="controlPlaneInfo">the control plane info.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="virtualMachineTokenProvider">Virtual machine token provider.</param>
        /// <param name="imageUrlGenerator">Image URL generator.</param>
        public CreateResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ICapacityManager capacityManager,
            IVirtualMachineTokenProvider virtualMachineTokenProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IImageUrlGenerator imageUrlGenerator)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            ControlPlaneInfo = controlPlaneInfo;
            CapacityManager = capacityManager;
            VirtualMachineTokenProvider = virtualMachineTokenProvider;
            ImageUrlGenerator = imageUrlGenerator;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICapacityManager CapacityManager { get; }

        private IVirtualMachineTokenProvider VirtualMachineTokenProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

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

            // Base resource tags that will be attached
            var resourceTags = new Dictionary<string, string>()
            {
                { ResourceTagName.ResourceId, resource.Value.Id ?? "unknown" },
                { ResourceTagName.ResourceType, resource.Value.Type.ToString() },
                { ResourceTagName.PoolLocation, resource.Value.Location ?? "unknown" },
                { ResourceTagName.PoolSkuName, resource.Value.PoolReference.Dimensions.GetValueOrDefault("skuName", "unknown") },
                { ResourceTagName.PoolDefinition, resource.Value.PoolReference.Code ?? "unknown" },
                { ResourceTagName.PoolVersionDefinition, resource.Value.PoolReference.VersionCode ?? "unknown" },
                { ResourceTagName.PoolImageFamilyName, resource.Value.PoolReference.Dimensions.GetValueOrDefault("imageFamilyName", "unknown") },
                { ResourceTagName.PoolImageName, resource.Value.PoolReference.Dimensions.GetValueOrDefault("imageName", "unknown") },
                { ResourceTagName.OperationReason, input.Reason ?? "unknown" },
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
                        new AzureResourceCriterion { ServiceType = ServiceType.Network, Quota = "VirtualNetworks", Required = 1 },
                    };
                    var resourceLocation = await SelectAzureResourceLocation(
                        criteria, input.ResourcePoolDetails.Location, logger.NewChildLogger());

                    // Get VM Agent Blob Url
                    var token = await VirtualMachineTokenProvider.GenerateAsync(resource.Value.Id, logger);
                    var url = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, computeDetails.VmAgentImageName);

                    // Add additional tags
                    resourceTags.Add(ResourceTagName.ComputeOS, computeDetails.OS.ToString());

                    result = new VirtualMachineProviderCreateInput
                    {
                        ResourceId = resource.Value.Id,
                        VMToken = token,
                        AzureVmLocation = computeDetails.Location,
                        AzureSkuName = computeDetails.SkuName,
                        AzureSubscription = Guid.Parse(resourceLocation.Subscription.SubscriptionId),
                        AzureResourceGroup = resourceLocation.ResourceGroup,
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
                    var resourceLocation = await SelectAzureResourceLocation(
                        criteria, input.ResourcePoolDetails.Location, logger.NewChildLogger());

                    var linuxCopyItem = new StorageCopyItem()
                    {
                        SrcBlobUrl = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, storageDetails.ImageName, TimeSpan.FromDays(100)),
                        StorageType = StorageType.Linux,
                    };

                    // The name of the Windows blob is implied by the name of the Linux blob.
                    // This is a limitation of the current schema for appsettings.images.json where only the image name is specified without knowledge of platform.
                    // This works because both the Windows and Linux blobs are pushed at the same time with the same version, the Windows blob just has the ".disk.vhdx" postfix.
                    var windowsCopyItem = new StorageCopyItem()
                    {
                        SrcBlobUrl = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, $"{storageDetails.ImageName}.disk.vhdx", TimeSpan.FromDays(100)),
                        StorageType = StorageType.Windows,
                    };

                    result = new FileShareProviderCreateInput
                    {
                        AzureLocation = storageDetails.Location.ToString().ToLowerInvariant(),
                        AzureSkuName = storageDetails.SkuName,
                        AzureSubscription = resourceLocation.Subscription.SubscriptionId,
                        AzureResourceGroup = resourceLocation.ResourceGroup,
                        StorageCopyItems = new[] { linuxCopyItem, windowsCopyItem },
                        ResourceTags = resourceTags,
                        StorageSizeInGb = storageDetails.SizeInGB,
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
                // Retry till we succeed
                await logger.RetryOperationScopeAsync(
                    $"{LogBaseName}_record_update",
                    async (IDiagnosticsLogger innerLogger) =>
                    {
                        resource.Value = (await ObtainReferenceAsync(input, innerLogger)).Value;

                        resource.Value.AzureResourceInfo = result.AzureResourceInfo;

                        resource.Value = await ResourceRepository.UpdateAsync(resource.Value, logger.WithValues(new LogValueSet()));
                    });
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
                    Dimensions = input.ResourcePoolDetails.GetPoolDimensions(),
                },
            };

            // Update input
            input.ResourceId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }

        private async Task<IAzureResourceLocation> SelectAzureResourceLocation(
            IEnumerable<AzureResourceCriterion> criteria, AzureLocation location, IDiagnosticsLogger logger)
        {
            try
            {
                // Check for capacity
                return await CapacityManager.SelectAzureResourceLocation(
                    criteria, location, logger.NewChildLogger());
            }
            catch (CapacityNotAvailableException ex)
            {
                // Translate to Temporarily Unavailable Exception
                throw new ContinuationTaskTemporarilyUnavailableException(
                    ex.Message, TimeSpan.FromMinutes(1), ex);
            }
        }
    }
}
