// <copyright file="BaseResourceImageTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Azure.Storage.Blob;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Contracts;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for background tasks that process all blobs/images.
    /// </summary>
    public abstract class BaseResourceImageTask : BaseBackgroundTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResourceImageTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="jobSchedulerFeatureFlags">The job scheduler feature flags instance.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public BaseResourceImageTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IJobSchedulerFeatureFlags jobSchedulerFeatureFlags,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            ResourceBrokerSettings = Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            JobSchedulerFeatureFlags = jobSchedulerFeatureFlags;
        }

        /// <summary>
        /// Gets the loop delay between each resource being processed.
        /// </summary>
        protected static TimeSpan LoopDelay { get; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets the base message name to use for log messages in the respective task.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets the name of the task. Usually the name of the implementing class.
        /// </summary>
        protected abstract string TaskName { get; }

        /// <summary>
        /// Gets the cutOff Date for deleting respective blobs.
        /// </summary>
        protected abstract DateTime CutOffTime { get; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{TaskName}Lease");

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private IJobSchedulerFeatureFlags JobSchedulerFeatureFlags { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public override void Dispose()
        {
            Disposed = true;
        }

        /// <inheritdoc/>
        protected override Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Fetch target images/blobs
                    var artifacts = GetArtifactTypesToCleanup();

                    // Run through found resources types (eg, VM/storage) in the background
                    await TaskHelper.RunEnumerableAsync(
                        $"{LogBaseName}_run_artifact_images",
                        artifacts,
                        (artifactFamilyType, itemLogger) => CoreRunArtifactAsync(artifactFamilyType, itemLogger),
                        childLogger,
                        (artifactFamilyType, itemLogger) => ObtainLeaseAsync($"{LeaseBaseName}-{artifactFamilyType}", taskInterval, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <summary>
        /// Gets the enumerable object for storage/VM agent images/blobs.
        /// </summary>
        /// <returns>IEnumerable obj for ImageFamilyType.</returns>
        protected abstract IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup();

        /// <summary>
        /// Gets the minumum number of blobs/images to be retained.
        /// </summary>
        /// <returns>container name.</returns>
        protected abstract int GetMinimumBlobCountToBeRetained();

        /// <summary>
        /// Gets the container name for the respective worker storage account.
        /// </summary>
        /// <returns>The name of the container.</returns>
        protected abstract string GetContainerName();

        /// <summary>
        /// Gets the Storage accounts to be processed.
        /// </summary>
        /// <returns>IEnumerable of SharedConnectionInfo objects.</returns>
        protected abstract Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync();

        /// <summary>
        /// Gets the activeImages.
        /// </summary>
        /// <param name="logger">Logger to be used.</param>
        /// <returns>IEnumerable of string that has active images.</returns>
        protected abstract Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the ImageFamilyType info.
        /// </summary>
        /// <returns>ImageFamilyType enum.</returns>
        protected abstract ImageFamilyType GetImageFamilyType();

        private async Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            if (await JobSchedulerFeatureFlags.IsFeatureFlagEnabledAsync(BaseResourceImageProducer.FeatureFlagName, BaseResourceImageProducer.DefaultEnabled))
            {
                return null;
            }

            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }

        private bool IsStorageArtifact()
        {
            return GetImageFamilyType().Equals(ImageFamilyType.Storage);
        }

        private async Task CoreRunArtifactAsync(ImageFamilyType artifactFamilyType, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ImageFamilyType", artifactFamilyType);

            // Tracking the task duration
            await logger.TrackDurationAsync(
                "RunArtifactAction", () => ProcessArtifactAsync(logger));
        }

        private async Task ProcessArtifactAsync(IDiagnosticsLogger logger)
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
                    var blobCountToBeRetained = GetMinimumBlobCountToBeRetained();
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
                    var isActiveImage = IsStorageArtifact() ?
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

                        // Slow down for rate limit & Database RUs
                        await Task.Delay(LoopDelay);
                    }
                });
        }
    }
}
