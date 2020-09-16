// <copyright file="BaseResourceImageJobHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.Jobs.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for all resource image job handlers.
    /// Note: we will allow the handler to try once per run to mimic the xisting task behavior
    /// </summary>
    /// <typeparam name="TJobHandlerType">Type of the job handler type.</typeparam>
    public abstract class BaseResourceImageJobHandler<TJobHandlerType> : JobHandlerPayloadBase<BaseResourceImageProducer.StorageAccountPayload<TJobHandlerType>>
        where TJobHandlerType : class
    {
        protected BaseResourceImageJobHandler()
            : base(options: new JobHandlerOptions() { MaxHandlerRetries = 1 })
        {
        }

        protected abstract ImageFamilyType ImageFamilyType { get; }

        /// <summary>
        /// Gets the base message name to use for log messages in the respective task.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the cutOff Date for deleting respective blobs.
        /// </summary>
        protected abstract DateTime CutOffTime { get; }

        /// <summary>
        /// Gets the minumum number of blobs/images to be retained.
        /// </summary>
        /// <returns>container name.</returns>
        protected abstract int MinimumBlobCountToBeRetained { get; }

        /// <inheritdoc/>
        protected override Task HandleJobAsync(BaseResourceImageProducer.StorageAccountPayload<TJobHandlerType> payload, IDiagnosticsLogger logger, CancellationToken cancellationToken)
        {
            return ProcessAccountAsync(payload.StorageAccount, logger);
        }

        /// <summary>
        /// Gets the container name for the respective worker storage account.
        /// </summary>
        /// <returns>The name of the container.</returns>
        protected abstract string GetContainerName();

        /// <summary>
        /// Gets the activeImages.
        /// </summary>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>IEnumerable of string that has active images.</returns>
        protected abstract Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger);

        private Task ProcessAccountAsync(ShareConnectionInfo account, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_artifacts",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue("ImageAccountName", account.StorageAccountName)
                        .FluentAddBaseValue("ImageTaskId", Guid.NewGuid());

                    // Fetch blob details
                    var containerName = GetContainerName();
                    var blobStorageClientOptions = new BlobStorageClientOptions
                    {
                        AccountName = account.StorageAccountName,
                        AccountKey = account.StorageAccountKey,
                    };
                    var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
                    var blobContainer = blobClientProvider.GetCloudBlobContainer(containerName);

                    // Fetch list of blobs
                    var blobsList = await blobContainer.ListBlobsSegmentedAsync(default);
                    var blobCountToBeRetained = MinimumBlobCountToBeRetained;
                    var activeImages = await GetActiveImagesAsync(logger);

                    childLogger
                        .FluentAddValue("ImageResultsFound", blobsList.Results.Count())
                        .FluentAddValue("ImageCountToBeRetained", blobCountToBeRetained);

                    // Doing this object conversion to access the created date of the blob. Since its is not accessible with IListBlobItem object.
                    var blobs = blobsList.Results.OfType<CloudBlockBlob>();
                    blobs = blobs.OrderByDescending((blob) => blob.Properties.LastModified);

                    for (var index = 0; index < blobs.Count(); index++)
                    {
                        await ProcessAccountBlobAsync(blobs.ElementAt(index), index, blobCountToBeRetained, activeImages, childLogger);
                    }
                });
        }

        private Task ProcessAccountBlobAsync(CloudBlockBlob blob, int index, int blobCountToBeRetained, IEnumerable<string> activeImage, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_images",
                async (childLogger) =>
                {
                    var isActiveImage = ImageFamilyType.Equals(ImageFamilyType.Storage) ?
                                        activeImage.Any(image => blob.Name.StartsWith(image, StringComparison.OrdinalIgnoreCase)) :
                                        activeImage.Any(image => blob.Name.Equals(image, StringComparison.OrdinalIgnoreCase));
                    var blobLastModifiedTimeInUtc = blob.Properties.LastModified.Value.UtcDateTime;
                    var isNewerThanCutoff = DateTime.Compare(blobLastModifiedTimeInUtc, CutOffTime) > 0;
                    var isToBeRetained = index < blobCountToBeRetained;
                    var shouldDelete = !isNewerThanCutoff && !isActiveImage && !isToBeRetained;

                    childLogger
                        .FluentAddValue("BlobLastModifiedTime", blobLastModifiedTimeInUtc)
                        .FluentAddValue("BlobCutoffTime", CutOffTime)
                        .FluentAddValue("BlobName", blob.Name)
                        .FluentAddValue("BlobIsNewerThanCutoff", isNewerThanCutoff)
                        .FluentAddValue("BlobIsActive", isActiveImage)
                        .FluentAddValue("BlobsToKeep", blobCountToBeRetained)
                        .FluentAddValue("BlobPosition", index)
                        .FluentAddValue("BlobShouldDelete", shouldDelete);

                    if (shouldDelete)
                    {
                        // Deleting the image blob from Azure storage account.
                        await blob.DeleteAsync();
                    }
                });
        }
    }
}
