// <copyright file="CreateStorageContinuationHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Repository;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Abstractions;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.VsSaaS.Services.CloudEnvironments.SystemCatalog.Abstractions;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Handlers
{
    public class CreateStorageContinuationHandler
        : CreateResourceContinuationHandler, ICreateStorageContinuationHandler
    {
        public CreateStorageContinuationHandler(
            IStorageProvider storageProvider,
            IAzureSubscriptionCatalog subscriptionCatalog,
            IResourceRepository resourceRepository,
            IMapper mapper,
            IBlobStorageClientProvider blobStorageClientProvider,
            ResourceBrokerSettings resourceBrokerSettings)
            : base(resourceRepository, subscriptionCatalog, mapper)
        {
            StorageProvider = storageProvider;
            BlobStorageClientProvider = blobStorageClientProvider;
            ResourceBrokerSettings = resourceBrokerSettings;
        }

        /// <inheritdoc/>
        protected override string TargetName => "JobCreateStorage";

        /// <inheritdoc/>
        protected override ResourceType TargetType => ResourceType.StorageFileShare;

        private IStorageProvider StorageProvider { get; }

        private IBlobStorageClientProvider BlobStorageClientProvider { get; }

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        /// <inheritdoc/>
        protected async override Task<ResourceCreateContinuationResult> CreateResourceAsync(CreateResourceContinuationInput input, string continuationToken, IDiagnosticsLogger logger)
        {
            var container = BlobStorageClientProvider.GetCloudBlobContainer(ResourceBrokerSettings.FileShareTemplateContainerName);
            var blob = container.GetBlobReference(ResourceBrokerSettings.FileShareTemplateBlobName);
            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(4), // This should be plenty of time to copy the blob template into the new file share
            });

            var providerInput = new FileShareProviderCreateInput
            {
                AzureLocation = input.Location,
                AzureSkuName = input.SkuName,
                AzureSubscription = input.Subscription,
                AzureResourceGroup = input.ResourceGroup,
                StorageBlobUrl = blob.Uri + sas,
            };

            return await StorageProvider.CreateAsync(providerInput, logger, continuationToken);
        }
    }
}
