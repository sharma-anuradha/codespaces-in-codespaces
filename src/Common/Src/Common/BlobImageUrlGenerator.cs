// <copyright file="BlobImageUrlGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Generates SAS tokens for Blobs.
    /// </summary>
    public class BlobImageUrlGenerator : IImageUrlGenerator
    {
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobImageUrlGenerator"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">Azure resource accessor.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="skuCatalog">SKU Catalog.</param>
        public BlobImageUrlGenerator(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog)
        {
            this.controlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
        }

        /// <inheritdoc/>
        public async Task<(string, string)> ReadOnlyUrlByVMFamily(
            AzureLocation location,
            ResourceType resourceType,
            string family,
            IDiagnosticsLogger logger,
            TimeSpan expiryTime = default)
        {
            var imageFamilies = skuCatalog.BuildArtifactVmAgentImageFamilies;
            if (!imageFamilies.ContainsKey(family))
            {
                return (null, null);
            }

            var blobName = await imageFamilies[family].GetCurrentImageNameAsync(logger);
            var url = await ReadOnlyUrlByImageName(location, resourceType, blobName, expiryTime);
            return (url, blobName);
        }

        /// <inheritdoc/>
        public async Task<string> ReadOnlyUrlByImageName(
            AzureLocation location,
            ResourceType resourceType,
            string imageName,
            TimeSpan expiryTime = default)
        {
            string accountName, accountKey, containerName;
            switch (resourceType)
            {
                case ResourceType.ComputeVM:
                    (accountName, accountKey) = await controlPlaneAzureResourceAccessor
                        .GetStampStorageAccountForComputeVmAgentImagesAsync(location);
                    containerName = controlPlaneInfo.VirtualMachineAgentContainerName;
                    break;
                case ResourceType.StorageFileShare:
                    (accountName, accountKey) = await controlPlaneAzureResourceAccessor
                        .GetStampStorageAccountForStorageImagesAsync(location);
                    containerName = controlPlaneInfo.FileShareTemplateContainerName;
                    break;
                default:
                    throw new ArgumentException($"Unhandled ResourceType: {resourceType}");
            }

            var blobStorageClientOptions = new BlobStorageClientOptions
            {
                AccountName = accountName,
                AccountKey = accountKey,
            };
            var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));

            if (expiryTime == default)
            {
                expiryTime = TimeSpan.FromHours(4);
            }

            var container = blobClientProvider.GetCloudBlobContainer(containerName);
            var blob = container.GetBlobReference(imageName);
            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow.Add(expiryTime),
            });

            return blob.Uri + sas;
        }
    }
}