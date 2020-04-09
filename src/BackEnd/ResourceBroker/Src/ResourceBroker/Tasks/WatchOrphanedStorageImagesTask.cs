// <copyright file="WatchOrphanedStorageImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// WatchOrphanedArtifactImagesTask to delete artifacts(kitchen sink images/blobs).
    /// </summary>
    public class WatchOrphanedStorageImagesTask : BaseResourceImageTask, IWatchOrphanedStorageImagesTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedStorageImagesTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Gets storage accounts.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        public WatchOrphanedStorageImagesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog)
            : base(
                  resourceBrokerSettings,
                  taskHelper,
                  claimedDistributedLease,
                  resourceNameBuilder)
        {
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
        }

        /// <inheritdoc/>
        protected override string TaskName { get; } = nameof(WatchOrphanedComputeImagesTask);

        /// <inheritdoc/>
        protected override string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedStorageImagesTask;

        /// <inheritdoc/>
        protected override DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        /// <summary>
        /// Gets the SkuCatalog to access the active image info.
        /// </summary>
        protected ISkuCatalog SkuCatalog { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <inheritdoc/>
        public async Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync()
        {
            var locations = ControlPlaneInfo.Stamp.DataPlaneLocations;
            var accounts = new List<ShareConnectionInfo>();

            foreach (var location in locations)
            {
                var (accountName, accountKey) = await ControlPlaneAzureResourceAccessor
                        .GetStampStorageAccountForStorageImagesAsync(location);
                var account = new ShareConnectionInfo();
                account.StorageAccountName = accountName;
                account.StorageAccountKey = accountKey;
                accounts.Add(account);
            }

            return accounts;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger)
        {
            var activeImages = new HashSet<string>();
            foreach (var item in SkuCatalog.BuildArtifactStorageImageFamilies.Values)
            {
                activeImages.Add(await item.GetCurrentImageNameAsync(logger));
            }

            return activeImages;
        }

        /// <inheritdoc/>
        public string GetContainerName()
        {
            return ControlPlaneInfo.FileShareTemplateContainerName;
        }

        /// <inheritdoc/>
        protected override IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.Storage, };
        }

        /// <inheritdoc/>
        protected override int GetMinimumCountToBeRetained() => 15;

        /// <inheritdoc/>
        protected override async Task ProcessArtifactAsync(IDiagnosticsLogger logger)
        {
            await logger.OperationScopeAsync(
                $"{LogBaseName}_run_process_accounts",
                async (childLogger) =>
                {
                    // Fetch accounts with account name and key
                    var accounts = await GetStorageAccountsAsync();
                    foreach (var account in accounts)
                    {
                        await ProcessAccountAsync(account, childLogger);
                    }
                },
                swallowException: true);
        }

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
                    var blobCountToBeRetained = GetMinimumCountToBeRetained();

                    childLogger
                        .FluentAddValue("ImageResultsFound", blobsList.Results.Count())
                        .FluentAddValue("ImageCounToBeRetained", blobCountToBeRetained);

                    // Doing this object conversion to access the created date of the blob. Since its is not accessible with IListBlobItem object.
                    var blobs = blobsList.Results.Cast<CloudBlockBlob>();
                    blobs = blobs.OrderByDescending((blob) => blob.Properties.Created);

                    for (var index = 0; index < blobs.Count(); index++)
                    {
                        await ProcessAccountBlobAsync(blobs.ElementAt(index), index, blobCountToBeRetained, await GetActiveImagesAsync(logger), childLogger);
                    }
                });
        }

        private Task ProcessAccountBlobAsync(CloudBlockBlob blob, int index, int blobCountToBeRetained, IEnumerable<string> activeImage, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_images",
                async (childLogger) =>
                {
                    var blobCreatedTimeInUtc = blob.Properties.Created.Value.UtcDateTime;
                    var isOutsideOfDateRange = DateTime.Compare(blobCreatedTimeInUtc, CutOffTime) > 0 ? true : false;
                    var isActiveImage = activeImage.Any(image => image.Equals(blob.Name, StringComparison.OrdinalIgnoreCase));
                    var isToBeRetained = index < blobCountToBeRetained;
                    var shouldDelete = !isOutsideOfDateRange && !isActiveImage && !isToBeRetained;

                    childLogger
                        .FluentAddValue("BlobCreatedTime", blobCreatedTimeInUtc)
                        .FluentAddValue("BlobCutoffTime", CutOffTime)
                        .FluentAddValue("BlobName", blob.Name)
                        .FluentAddValue("BlobIsOutsideOfDateRange", isOutsideOfDateRange)
                        .FluentAddValue("BlobIsActiveImage", isActiveImage)
                        .FluentAddValue("BlobIsToBeRetained", isToBeRetained)
                        .FluentAddValue("BlobShouldDelete", shouldDelete);

                    if (shouldDelete)
                    {
                        // Deleting the image blob from Azure storage account.
                        await blob.DeleteAsync();

                        // Slow down for rate limit & Database RUs
                        await Task.Delay(LoopDelay);
                    }
                });
        }
    }
}
