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
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;
using Microsoft.VsSaaS.Services.CloudEnvironments.StorageFileShareProvider.Models;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// Base class for background tasks that process all blobs for the respective storage accounts.
    /// </summary>
    public abstract class BaseResourceImageTask : IBackgroundTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseResourceImageTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneAzureResourceAccessor">Gets storage accounts.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        public BaseResourceImageTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            IControlPlaneAzureResourceAccessor controlPlaneAzureResourceAccessor,
            ISkuCatalog skuCatalog)
        {
            ResourceBrokerSettings = Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureResourceAccessor = Requires.NotNull(controlPlaneAzureResourceAccessor, nameof(controlPlaneAzureResourceAccessor));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
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
        /// Gets the SkuCatalog to access the active image info.
        /// </summary>
        protected ISkuCatalog SkuCatalog { get; }

        /// <summary>
        /// Gets controPlane info.
        /// </summary>
        protected IControlPlaneInfo ControlPlaneInfo { get; }

        /// <summary>
        /// Gets Control plane accessor.
        /// </summary>
        protected IControlPlaneAzureResourceAccessor ControlPlaneAzureResourceAccessor { get; }

        /// <summary>
        /// Gets the cutOff Date for deleting respective blobs.
        /// </summary>
        protected abstract DateTime CutOffDate { get; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{TaskName}Lease");

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

        private bool Disposed { get; set; }

        /// <inheritdoc/>
        public Task<bool> RunAsync(TimeSpan taskInterval, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run",
                async (childLogger) =>
                {
                    // Fetch target images/blobs
                    var activeImages = await GetActiveImagesAsync(logger);
                    var artifacts = GetArtifactTypesToCleanup();

                    // Run through found resources types (eg, VM/storage) in the background
                    await TaskHelper.RunEnumerableAsync(
                        $"{LogBaseName}_run_artifact_images",
                        artifacts,
                        (artifactFamilyType, itemLogger) => CoreRunArtifactAsync(activeImages, artifactFamilyType, itemLogger),
                        childLogger,
                        (artifactFamilyType, itemLogger) => ObtainLease($"{LeaseBaseName}-{artifactFamilyType}", taskInterval, itemLogger));

                    return !Disposed;
                },
                (e, childLogger) => Task.FromResult(!Disposed),
                swallowException: true);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Disposed = true;
        }

        /// <summary>
        /// Gets the account name & Key for the storage account.
        /// </summary>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        protected abstract Task<IEnumerable<ShareConnectionInfo>> GetStorageAccountsAsync();

        /// <summary>
        /// Gets all the Images/Blobs that are being currently used form SkuCatalog.
        /// </summary>
        /// <param name="logger">The logger which should be used.</param>
        /// <returns>A Task representing the completion of the processing logic.</returns>
        protected abstract Task<IEnumerable<string>> GetActiveImagesAsync(IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the enumerable object for storage/VM agent images/blobs.
        /// </summary>
        /// <returns>IEnumerable obj for ImageFamilyType.</returns>
        protected abstract IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup();

        private async Task<IDisposable> ObtainLease(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return await ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }

        private async Task CoreRunArtifactAsync(IEnumerable<string> activeImages, ImageFamilyType artifactFamilyType, IDiagnosticsLogger logger)
        {
            logger.FluentAddBaseValue("ImageFamilyType", artifactFamilyType);

            // Tracking the task duration
            await logger.TrackDurationAsync(
                "RunArtifactAction", () => ProcessArtifactAsync(activeImages, logger));
        }

        private async Task ProcessArtifactAsync(IEnumerable<string> activeImage, IDiagnosticsLogger logger)
        {
            // Fetch accounts with account name and key
            var accounts = await GetStorageAccountsAsync();

            foreach (var account in accounts)
            {
                // Fetch blob details
                var containerName = ControlPlaneInfo.FileShareTemplateContainerName;
                var blobStorageClientOptions = new BlobStorageClientOptions
                {
                    AccountName = account.StorageAccountName,
                    AccountKey = account.StorageAccountKey,
                };
                var blobClientProvider = new BlobStorageClientProvider(Options.Create(blobStorageClientOptions));
                var blobContainer = blobClientProvider.GetCloudBlobContainer(containerName);

                // Fetch list of blobs
                var blobsList = await blobContainer.ListBlobsSegmentedAsync(default);

                logger.FluentAddValue("ImageTargetResultsFound", blobsList.Results.Count());

                // Run through each item and process
                foreach (var blob in blobsList.Results)
                {
                    var blockBlob = (CloudBlockBlob)blob;

                    // Determine if we should delete (filter it out more than cenrtain days old && not being currently used)
                    var shouldDelete = blockBlob.Properties.Created < CutOffDate && !activeImage.Any(item => item.Equals(blockBlob.Name, StringComparison.OrdinalIgnoreCase));

                    if (shouldDelete)
                    {
                        logger.FluentAddValue("ImageBlobName", blockBlob.Name);

                        // Deleting the image blob from Azure storage account.
                        await blockBlob.DeleteAsync();

                        // Slow down for rate limit & Database RUs
                        await Task.Delay(LoopDelay);
                    }
                }
            }
        }
    }
}
