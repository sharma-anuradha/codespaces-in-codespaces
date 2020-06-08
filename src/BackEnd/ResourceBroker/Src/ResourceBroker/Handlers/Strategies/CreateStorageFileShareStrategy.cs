// <copyright file="CreateStorageFileShareStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Create resource storage file share strategy.
    /// </summary>
    public class CreateStorageFileShareStrategy : ICreateResourceStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateStorageFileShareStrategy"/> class.
        /// </summary>
        /// <param name="storageProvider">Storage provider.</param>
        /// <param name="imageUrlGenerator">Image url generator.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        public CreateStorageFileShareStrategy(
            IStorageProvider storageProvider,
            IImageUrlGenerator imageUrlGenerator,
            ICapacityManager capacityManager)
        {
            StorageProvider = Requires.NotNull(storageProvider, nameof(storageProvider));
            ImageUrlGenerator = Requires.NotNull(imageUrlGenerator, nameof(imageUrlGenerator));
            CapacityManager = Requires.NotNull(capacityManager, nameof(capacityManager));
        }

        private IStorageProvider StorageProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

        private ICapacityManager CapacityManager { get; }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.StorageFileShare;
        }

        /// <inheritdoc/>
        public async Task<ContinuationInput> BuildCreateOperationInputAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            // Base resource tags that will be attached
            var resourceTags = resource.Value.GetResourceTags(input.Reason);
            ContinuationInput result;

            if (resource.Value.Type == ResourceType.StorageFileShare)
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
                        SrcBlobUrl = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, storageDetails.ImageName, logger.NewChildLogger(), TimeSpan.FromDays(100)),
                        StorageType = StorageType.Linux,
                    };

                    // The name of the Windows blob is implied by the name of the Linux blob.
                    // This is a limitation of the current schema for appsettings.images.json where only the image name is specified without knowledge of platform.
                    // This works because both the Windows and Linux blobs are pushed at the same time with the same version, the Windows blob just has the ".disk.vhdx" postfix.
                    var windowsCopyItem = new StorageCopyItem()
                    {
                        SrcBlobUrl = await ImageUrlGenerator.ReadOnlyUrlByImageName(input.ResourcePoolDetails.Location, resource.Value.Type, $"{storageDetails.ImageName}.disk.vhdx", logger.NewChildLogger(), TimeSpan.FromDays(100)),
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
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            ResourceCreateContinuationResult result;

            // Run create operation
            if (resource.Value.Type == ResourceType.StorageFileShare)
            {
                result = await StorageProvider.CreateAsync((FileShareProviderCreateInput)input.OperationInput, logger.NewChildLogger());
            }
            else
            {
                throw new NotSupportedException($"Resource type is not selected - {resource.Value.Type}");
            }

            return result;
        }
    }
}