// <copyright file="CreateResourceContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Auth;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEnd.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ComputeVirtualMachineProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.DiskProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
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
        /// <param name="keyVaultProvider">The keyvault provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Target control plane resource accessor.</param>
        /// <param name="computeProvider">Compute provider.</param>
        /// <param name="storageProvider">Storatge provider.</param>
        /// <param name="controlPlaneAzureResourceAccessor">the control plane resource accessor.</param>
        /// <param name="controlPlaneInfo">the control plane info.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        /// <param name="resourceRepository">Resource repository to be used.</param>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="tokenProvider">Token provider.</param>
        /// <param name="imageUrlGenerator">Image URL generator.</param>
        /// <param name="azureSubscriptionCatalog">Subscription catalog.</param>
        /// <param name="diskProvider">Disk provider.</param>
        public CreateResourceContinuationHandler(
            IComputeProvider computeProvider,
            IStorageProvider storageProvider,
            IKeyVaultProvider keyVaultProvider,
            IDiskProvider diskProvider,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ICapacityManager capacityManager,
            ITokenProvider tokenProvider,
            IResourceRepository resourceRepository,
            IServiceProvider serviceProvider,
            IImageUrlGenerator imageUrlGenerator,
            IAzureSubscriptionCatalog azureSubscriptionCatalog)
            : base(serviceProvider, resourceRepository)
        {
            ComputeProvider = computeProvider;
            StorageProvider = storageProvider;
            KeyVaultProvider = keyVaultProvider;
            ControlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            ControlPlaneInfo = controlPlaneInfo;
            CapacityManager = capacityManager;
            TokenProvider = tokenProvider;
            ImageUrlGenerator = imageUrlGenerator;
            AzureSubscriptionCatalog = azureSubscriptionCatalog;
            DiskProvider = diskProvider;
        }

        /// <inheritdoc/>
        protected override string LogBaseName => ResourceLoggingConstants.ContinuationTaskMessageHandlerCreate;

        /// <inheritdoc/>
        protected override string DefaultTarget => DefaultQueueTarget;

        /// <inheritdoc/>
        protected override ResourceOperation Operation => ResourceOperation.Provisioning;

        private IComputeProvider ComputeProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IKeyVaultProvider KeyVaultProvider { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private ICapacityManager CapacityManager { get; }

        private ITokenProvider TokenProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

        private IAzureSubscriptionCatalog AzureSubscriptionCatalog { get; }

        private IDiskProvider DiskProvider { get; }

        /// <inheritdoc/>
        protected override async Task<ResourceRecordRef> FetchReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
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
            var resourceTags = resource.Value.GetResourceTags(input.Reason);

            if (resource.Value.Type == ResourceType.ComputeVM)
            {
                // Ensure that the details type is correct
                if (input.ResourcePoolDetails is ResourcePoolComputeDetails computeDetails)
                {
                    var components = new List<ResourceComponent>();
                    var resourceLocation = default(IAzureResourceLocation);
                    var existingOSDisk = default(ResourceRecord);

                    if (input.Options is CreateComputeContinuationInputOptions computeOption)
                    {
                        var osDiskId = computeOption.OSDiskResourceId;

                        if (osDiskId != default)
                        {
                            existingOSDisk = await ResourceRepository.GetAsync(osDiskId, logger.NewChildLogger());

                            components.Add(new ResourceComponent(ResourceType.OSDisk, existingOSDisk.AzureResourceInfo, osDiskId));
                        }

                        if (existingOSDisk?.AzureResourceInfo?.Name != default)
                        {
                            // Creates VM with an already existing resource.
                            // TODO: janraj, this overrides the criteria based selection, because the OSDisk is still in the same place it was originally created. Future WI.
                            // TODO: janraj, copy disk to target subscription and create VM. copying azure managed disks takes ~10 seconds.
                            var azureSubscription = AzureSubscriptionCatalog.AzureSubscriptions.Single(x => x.SubscriptionId == existingOSDisk.AzureResourceInfo.SubscriptionId.ToString());
                            var azureLocation = existingOSDisk.Location.ToEnum<AzureLocation>();

                            resourceLocation = new AzureResourceLocation(
                                azureSubscription,
                                existingOSDisk.AzureResourceInfo.ResourceGroup,
                                azureLocation);
                        }
                    }

                    if (resourceLocation == default)
                    {
                        // Set up the selection criteria and select a subscription/location.
                        var criteria = new List<AzureResourceCriterion>
                        {
                            // SkuFamily must be first as the primary criterion for ordering candidate subscriptions.
                            new AzureResourceCriterion { ServiceType = ServiceType.Compute, Quota = computeDetails.SkuFamily, Required = computeDetails.Cores },
                            new AzureResourceCriterion { ServiceType = ServiceType.Network, Quota = "VirtualNetworks", Required = 1 },
                        };

                        resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                            criteria, input.ResourcePoolDetails.Location, logger.NewChildLogger());
                    }

                    // Get VM Agent Blob Url
                    var token = await TokenProvider.GenerateVmTokenAsync(resource.Value.Id, logger);
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
                        CustomComponents = components,
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
            else if (resource.Value.Type == ResourceType.KeyVault)
            {
                if (input.ResourcePoolDetails is ResourcePoolKeyVaultDetails keyVaultDetails)
                {
                    // Set up the selection criteria and select a subscription/location.
                    var criteria = new List<AzureResourceCriterion>
                    {
                        new AzureResourceCriterion { ServiceType = ServiceType.KeyVault, Quota = ServiceType.KeyVault.ToString(), Required = 1 },
                    };

                    var resourceLocation = await CapacityManager.SelectAzureResourceLocation(
                        criteria, keyVaultDetails.Location, logger.NewChildLogger());

                    result = new KeyVaultProviderCreateInput
                    {
                        ResourceId = resource.Value.Id,
                        AzureLocation = keyVaultDetails.Location,
                        AzureSkuName = keyVaultDetails.SkuName,
                        AzureSubscriptionId = resourceLocation.Subscription.SubscriptionId,
                        AzureTenantId = resourceLocation.Subscription.ServicePrincipal.TenantId,
                        AzureObjectId = resourceLocation.Subscription.ServicePrincipal.ObjectId,
                        AzureResourceGroup = resourceLocation.ResourceGroup,
                        ResourceTags = resourceTags,
                    };
                }
                else
                {
                    throw new NotSupportedException($"Pool keyvault details type is not selected - {input.ResourcePoolDetails.GetType()}");
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
                result = await ComputeProvider.CreateAsync((VirtualMachineProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                result = await StorageProvider.CreateAsync((FileShareProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else if (resource.Value.Type == ResourceType.KeyVault)
            {
                result = await KeyVaultProvider.CreateAsync((KeyVaultProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            // Make sure we bring over the Resource info if we have it
            if (result.AzureResourceInfo != default)
            {
                // Update the components provisioning status.
                if (result.Status == OperationState.Succeeded && result.Components?.Items != default)
                {
                    foreach (var component in result.Components.Items.Values)
                    {
                        if (component.ResourceRecordId != default)
                        {
                            var componentResourceReference = await FetchReferenceAsync(Guid.Parse(component.ResourceRecordId), logger.NewChildLogger());

                            if (component.AzureResourceInfo == default)
                            {
                                component.AzureResourceInfo = await AcquireComponentAzureResourceAsync(resource, result, logger, component, componentResourceReference);

                                if (componentResourceReference.Value.ProvisioningStatus != result.Status ||
                                    !component.AzureResourceInfo.Equals(componentResourceReference.Value.AzureResourceInfo))
                                {
                                    await UpdateRecordAsync(
                                        input,
                                        componentResourceReference,
                                        (componentRecord, childLogger) =>
                                        {
                                            componentRecord.ProvisioningStatus = result.Status;
                                            componentRecord.ProvisioningStatusChanged = DateTime.UtcNow;
                                            componentRecord.AzureResourceInfo = component.AzureResourceInfo;

                                            return true;
                                        },
                                        logger.NewChildLogger());
                                }
                            }
                        }
                    }
                }

                if (resource.Value.AzureResourceInfo == default
                    || !resource.Value.AzureResourceInfo.Equals(result.AzureResourceInfo)
                    || (result.Components != default && (resource.Value.Components == default || !resource.Value.Components.Equals(result.Components))))
                {
                    // Retry till we succeed
                    await logger.RetryOperationScopeAsync(
                        $"{LogBaseName}_record_update",
                        async (IDiagnosticsLogger innerLogger) =>
                        {
                            resource.Value = (await FetchReferenceAsync(input, innerLogger)).Value;

                            resource.Value.AzureResourceInfo = result.AzureResourceInfo;

                            resource.Value.Components = result.Components;

                            resource.Value = await ResourceRepository.UpdateAsync(resource.Value, innerLogger.NewChildLogger());
                        });
                }
            }

            return result;
        }

        private async Task<AzureResourceInfo> AcquireComponentAzureResourceAsync(
            ResourceRecordRef resource,
            ResourceCreateContinuationResult resourceCreateContinuationResult,
            IDiagnosticsLogger logger,
            ResourceComponent component,
            ResourceRecordRef componentResourceReference)
        {
            if (component.ComponentType == ResourceType.OSDisk)
            {
                var computeResourceTags = new Dictionary<string, string>
                {
                    [ResourceTagName.ResourceComponentRecordIds] = componentResourceReference.Value.Id,
                };

                var diskResourceResult = await DiskProvider.AcquireOSDiskAsync(
                    new DiskProviderAcquireOSDiskInput()
                    {
                        VirtualMachineResourceInfo = resourceCreateContinuationResult.AzureResourceInfo,
                        AzureVmLocation = resource.Value.Location.ToEnum<AzureLocation>(),
                        OSDiskResourceTags = componentResourceReference.Value.GetResourceTags("QueuedAllocation"),
                        AdditionalComputeResourceTags = computeResourceTags,
                    },
                    logger.NewChildLogger());

                return diskResourceResult.AzureResourceInfo;
            }
            else
            {
                // No other case for now.
                return default;
            }
        }

        private async Task<ResourceRecordRef> CreateReferenceAsync(CreateResourceContinuationInput input, IDiagnosticsLogger logger)
        {
            // Common properties
            var id = input.ResourceId;
            var time = DateTime.UtcNow;
            var type = input.Type;
            var location = input.ResourcePoolDetails.Location;
            var skuName = input.ResourcePoolDetails.SkuName;
            var poolReference = new ResourcePoolDefinitionRecord
            {
                Code = input.ResourcePoolDetails.GetPoolDefinition(),
                VersionCode = input.ResourcePoolDetails.GetPoolVersionDefinition(),
                Dimensions = input.ResourcePoolDetails.GetPoolDimensions(),
            };

            // Build core record
            var record = ResourceRecord.Build(id, time, type, location, skuName, poolReference);
            if (input.IsAssigned)
            {
                record.IsAssigned = true;
                record.Assigned = DateTime.UtcNow;
            }

            // Update input
            input.ResourceId = id;

            // Create the actual record
            record = await ResourceRepository.CreateAsync(record, logger);

            return new ResourceRecordRef(record);
        }
    }
}
