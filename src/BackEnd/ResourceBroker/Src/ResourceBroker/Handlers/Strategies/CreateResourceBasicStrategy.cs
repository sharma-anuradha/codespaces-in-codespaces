// <copyright file="CreateResourceBasicStrategy.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Capacity.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Continuation;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider;
using Microsoft.VsSaaS.Services.CloudEnvironments.KeyVaultProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    /// <summary>
    /// Create resource basic strategy.
    /// </summary>
    public class CreateResourceBasicStrategy : ICreateResourceStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateResourceBasicStrategy"/> class.
        /// </summary>
        /// <param name="storageProvider">Storage provider.</param>
        /// <param name="keyVaultProvider">Keyvault provider.</param>
        /// <param name="imageUrlGenerator">Image url generator.</param>
        /// <param name="capacityManager">The capacity manager.</param>
        public CreateResourceBasicStrategy(
            IStorageProvider storageProvider,
            IKeyVaultProvider keyVaultProvider,
            IImageUrlGenerator imageUrlGenerator,
            ICapacityManager capacityManager)
        {
            KeyVaultProvider = Requires.NotNull(keyVaultProvider, nameof(keyVaultProvider));
            StorageProvider = Requires.NotNull(storageProvider, nameof(storageProvider));
            ImageUrlGenerator = Requires.NotNull(imageUrlGenerator, nameof(imageUrlGenerator));
            CapacityManager = Requires.NotNull(capacityManager, nameof(capacityManager));
        }

        private IKeyVaultProvider KeyVaultProvider { get; }

        private IStorageProvider StorageProvider { get; }

        private IImageUrlGenerator ImageUrlGenerator { get; }

        private ICapacityManager CapacityManager { get; }

        /// <inheritdoc/>
        public bool CanHandle(CreateResourceContinuationInput input)
        {
            return input.Type == ResourceType.StorageFileShare
                && input.Type == ResourceType.KeyVault;
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
            else if (resource.Value.Type == ResourceType.KeyVault)
            {
                if (input.ResourcePoolDetails is ResourcePoolKeyVaultDetails keyVaultDetails)
                {
                    // Set up the selection criteria and select a subscription/location.
                    var criteria = new List<AzureResourceCriterion>
                    {
                        new AzureResourceCriterion { ServiceType = ServiceType.KeyVault, Quota = ServiceType.KeyVault.ToString(), Required = 1 },
                    };

                    var resourceLocation = await SelectAzureResourceLocation(
                        criteria, keyVaultDetails.Location, logger.NewChildLogger());

                    result = new KeyVaultProviderCreateInput
                    {
                        ResourceId = resource.Value.Id,
                        AzureLocation = keyVaultDetails.Location,
                        AzureSkuName = keyVaultDetails.SkuName,
                        AzureSubscriptionId = resourceLocation.Subscription.SubscriptionId,
                        AzureTenantId = resourceLocation.Subscription.ServicePrincipal.TenantId,
                        AzureObjectId = resourceLocation.Subscription.ServicePrincipal.ClientId,
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
        public async Task<ResourceCreateContinuationResult> RunCreateOperationCoreAsync(CreateResourceContinuationInput input, ResourceRecordRef resource, IDiagnosticsLogger logger)
        {
            ResourceCreateContinuationResult result;

            // Run create operation
            if (resource.Value.Type == ResourceType.StorageFileShare)
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

            return result;
        }

        /// <summary>
        /// Select azure resource location.
        /// </summary>
        /// <param name="criteria">criteria.</param>
        /// <param name="location">location.</param>
        /// <param name="logger">logger.</param>
        /// <returns>result.</returns>
        protected async Task<IAzureResourceLocation> SelectAzureResourceLocation(
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