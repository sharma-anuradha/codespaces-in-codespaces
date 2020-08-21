// <copyright file="BlobImageUrlGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Caching;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <summary>
    /// Generates SAS tokens for Blobs.
    /// </summary>
    public class BlobImageUrlGenerator : IImageUrlGenerator
    {
        private const string LogBaseName = "blob_image_url_generator";
        private readonly IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor;
        private readonly IControlPlaneInfo controlPlaneInfo;
        private readonly ISkuCatalog skuCatalog;
        private readonly IManagedCache cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobImageUrlGenerator"/> class.
        /// </summary>
        /// <param name="controlPlaneAzureResourceAccessor">Azure resource accessor.</param>
        /// <param name="controlPlaneInfo">Control plane info.</param>
        /// <param name="skuCatalog">SKU Catalog.</param>
        /// <param name="cache">The cache to store responses.</param>
        public BlobImageUrlGenerator(
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            IManagedCache cache)
        {
            this.controlPlaneAzureResourceAccessor = controlPlaneAzureResourceAccessor;
            this.controlPlaneInfo = controlPlaneInfo;
            this.skuCatalog = skuCatalog;
            this.cache = cache;
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
            var url = await ReadOnlyUrlByImageName(location, resourceType, blobName, logger.NewChildLogger(), expiryTime);
            return (url, blobName);
        }

        /// <inheritdoc/>
        public async Task<string> ReadOnlyUrlByImageName(
            AzureLocation location,
            ResourceType resourceType,
            string imageName,
            IDiagnosticsLogger logger,
            TimeSpan expiryTime = default)
        {
            return await logger.OperationScopeAsync(
                $"{LogBaseName}_get_read_only_url_by_image_name",
                async (childLogger) =>
                {
                    string accountName, accountKey, containerName;
                    CloudBlobContainer container;

                    switch (resourceType)
                    {
                        case ResourceType.ComputeVM:
                            containerName = controlPlaneInfo.VirtualMachineAgentContainerName;
                            break;
                        case ResourceType.StorageFileShare:
                            containerName = controlPlaneInfo.FileShareTemplateContainerName;
                            break;
                        default:
                            throw new ArgumentException($"Unhandled ResourceType: {resourceType}");
                    }

                    var containerCacheKey = GetCacheKey(location, resourceType, containerName);
                    container = await cache.GetAsync<CloudBlobContainer>(containerCacheKey, logger);

                    if (container == null)
                    {
                        switch (resourceType)
                        {
                            case ResourceType.ComputeVM:
                                (accountName, accountKey) = await controlPlaneAzureResourceAccessor
                                    .GetStampStorageAccountForComputeVmAgentImagesAsync(location);
                                break;
                            case ResourceType.StorageFileShare:
                                (accountName, accountKey) = await controlPlaneAzureResourceAccessor
                                    .GetStampStorageAccountForStorageImagesAsync(location);
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
                        container = blobClientProvider.GetCloudBlobContainer(containerName);
                        await cache.SetAsync(containerCacheKey, container, TimeSpan.FromHours(24), logger);
                    }

                    if (expiryTime == default)
                    {
                        expiryTime = TimeSpan.FromHours(4);
                    }

                    var blob = container.GetBlobReference(imageName);

#pragma warning disable CA5377 // Use Container Level Access Policy
                    var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                    {
                        Permissions = SharedAccessBlobPermissions.Read,
                        SharedAccessExpiryTime = DateTime.UtcNow.Add(expiryTime),
                    });
#pragma warning restore CA5377 // Use Container Level Access Policy

                    return blob.Uri + sas;
                });
        }

        private string GetCacheKey(AzureLocation location, ResourceType resourceType, string resourceName)
        {
            return $"{nameof(BlobImageUrlGenerator)}:{location.ToString().ToLowerInvariant()}:{resourceType}:{resourceName}";
        }
    }
}