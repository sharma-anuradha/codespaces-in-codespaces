// <copyright file="WatchOrphanedComputeImagesTask.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Rest.Azure;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration.KeyGenerator;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Settings;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ResourceBroker.Tasks
{
    /// <summary>
    /// WatchOrphanedComputeImagesTask to delete artifacts(Nexus windows images).
    /// </summary>
    public class WatchOrphanedComputeImagesTask : BaseBackgroundTask
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WatchOrphanedComputeImagesTask"/> class.
        /// </summary>
        /// <param name="resourceBrokerSettings">Target resource broker settings.</param>
        /// <param name="taskHelper">Task helper.</param>
        /// <param name="claimedDistributedLease">Claimed distributed lease.</param>
        /// <param name="resourceNameBuilder">Resource name builder.</param>
        /// <param name="controlPlaneInfo">Gets control plan info.</param>
        /// <param name="skuCatalog">Gets skuCatalog that has active image info.</param>
        /// <param name="controlPlaneAzureClientFactory">Azure Client Factory for control plane related works.</param>
        /// <param name="configurationReader">Configuration reader.</param>
        public WatchOrphanedComputeImagesTask(
            ResourceBrokerSettings resourceBrokerSettings,
            ITaskHelper taskHelper,
            IClaimedDistributedLease claimedDistributedLease,
            IResourceNameBuilder resourceNameBuilder,
            IControlPlaneInfo controlPlaneInfo,
            ISkuCatalog skuCatalog,
            IControlPlaneAzureClientFactory controlPlaneAzureClientFactory,
            IConfigurationReader configurationReader)
            : base(configurationReader)
        {
            ResourceBrokerSettings = Requires.NotNull(resourceBrokerSettings, nameof(resourceBrokerSettings));
            TaskHelper = Requires.NotNull(taskHelper, nameof(taskHelper));
            ClaimedDistributedLease = Requires.NotNull(claimedDistributedLease, nameof(claimedDistributedLease));
            ResourceNameBuilder = Requires.NotNull(resourceNameBuilder, nameof(resourceNameBuilder));
            SkuCatalog = Requires.NotNull(skuCatalog, nameof(skuCatalog));
            ControlPlaneInfo = Requires.NotNull(controlPlaneInfo, nameof(controlPlaneInfo));
            ControlPlaneAzureClientFactory = Requires.NotNull(controlPlaneAzureClientFactory, nameof(controlPlaneAzureClientFactory));
        }

        /// <summary>
        /// Gets the loop delay between each resource being processed.
        /// </summary>
        private static TimeSpan LoopDelay { get; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets the minimum image count to be retained.
        /// for example, active image versions are ["18.04-LTS:latest","2020.0316.001","2020.0316.501","2020.0317.701","2020.0316.601"]
        /// we should always have the minimum count as multiplier of the size of the active images array to maintain minimum images
        /// for each active image versions.
        /// </summary>
        /// <returns>Returns the count of minimum images to be retained.</returns>
        private int MinimumImageCountToBeRetained => 40;

        private string TaskName { get; } = nameof(WatchOrphanedComputeImagesTask);

        /// <inheritdoc/>
        protected override string ConfigurationBaseName => "WatchOrphanedComputeImagesTask";

        private string LogBaseName { get; } = ResourceLoggingConstants.WatchOrphanedComputeImagesTask;

        private DateTime CutOffTime => DateTime.Now.AddMonths(-1).ToUniversalTime();

        private ISkuCatalog SkuCatalog { get; }

        private IControlPlaneInfo ControlPlaneInfo { get; }

        private IControlPlaneAzureClientFactory ControlPlaneAzureClientFactory { get; }

        private string LeaseBaseName => ResourceNameBuilder.GetLeaseName($"{TaskName}Lease");

        private ResourceBrokerSettings ResourceBrokerSettings { get; }

        private ITaskHelper TaskHelper { get; }

        private IClaimedDistributedLease ClaimedDistributedLease { get; }

        private IResourceNameBuilder ResourceNameBuilder { get; }

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
                $"{LogBaseName}_run_locations",
                async (childLogger) =>
                {
                    // Fetch all possible locations.
                    var locations = ControlPlaneInfo.Stamp.DataPlaneLocations;
                    var computeManagementClient = await ControlPlaneAzureClientFactory.GetComputeManagementClient();

                    foreach (var location in locations)
                    {
                        await ProcessByLocationsAsync(computeManagementClient, location, childLogger);
                    }
                },
                swallowException: true);
        }

        private Task ProcessByLocationsAsync(IComputeManagementClient computeManagementClient, AzureLocation location, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_run_image_definitions",
                async (childLogger) =>
                {
                    childLogger
                        .FluentAddBaseValue("ImageLocation", location)
                        .FluentAddBaseValue("ImageTaskId", Guid.NewGuid());

                    var resourceGroupName = ControlPlaneInfo.Stamp.GetResourceGroupNameForCustomVmImages(location);
                    var galleryName = ControlPlaneInfo.Stamp.GetImageGalleryNameForCustomVmImages(location);
                    var imageDefinitions = new HashSet<GalleryImageInner>();
                    var imageDefinitionSubList = default(IPage<GalleryImageInner>);
                    var nextPagelink = string.Empty;

                    do
                    {
                        imageDefinitionSubList = await computeManagementClient.GalleryImages.ListByGalleryAsync(resourceGroupName, galleryName);
                        nextPagelink = imageDefinitionSubList.NextPageLink;

                        // Slow down for rate limit & Database RUs
                        await Task.Delay(LoopDelay);

                        foreach (var obj in imageDefinitionSubList)
                        {
                            imageDefinitions.Add(obj);
                        }
                    }
                    while (nextPagelink != null);

                    // Process all the image definitions.
                    foreach (var imageDefinition in imageDefinitions)
                    {
                        await ProcessImageDefinitionsAsync(computeManagementClient, resourceGroupName, galleryName, imageDefinition.Name, childLogger);
                    }

                    // Process orphaned images
                    await ProcessOrphanImageAsync(computeManagementClient, resourceGroupName, galleryName, imageDefinitions, childLogger);
                });
        }

        private Task ProcessImageDefinitionsAsync(IComputeManagementClient computeManagementClient, string resourceGroupName, string galleryName, string imageDefinitionName, IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
               $"{LogBaseName}_run_images_versions",
               async (childLogger) =>
               {
                   var activeImageVersions = await GetActiveImageVersionsAsync(logger);

                   // Fetch all the ImageInfo
                   var lookupImageInfo = await FetchLookupImageInfo(computeManagementClient, resourceGroupName, galleryName, imageDefinitionName);

                   // sorting to retain the most recent images using index
                   var sortedImageVersions = lookupImageInfo.OrderByDescending((versions) => versions.Key.Name).ToList();

                   for (var index = 0; index < sortedImageVersions.Count(); index++)
                   {
                       await ProcessImagesAndVersionsAsync(
                           computeManagementClient,
                           resourceGroupName,
                           galleryName,
                           imageDefinitionName,
                           sortedImageVersions.ElementAt(index),
                           index,
                           MinimumImageCountToBeRetained,
                           activeImageVersions,
                           childLogger);
                   }
               });
        }

        private Task ProcessImagesAndVersionsAsync(
            IComputeManagementClient computeManagementClient,
            string resourceGroupName,
            string galleryName,
            string imageDefinitionName,
            KeyValuePair<GalleryImageVersionInner, string> keyValuePair,
            int index,
            int imageIndexToBeRetained,
            IEnumerable<string> activeImageVersions,
            IDiagnosticsLogger logger)
        {
            return logger.OperationScopeAsync(
                $"{LogBaseName}_delete_images_and_versions",
                async (childLogger) =>
                {
                    var imageVersion = keyValuePair.Key;
                    var imageVersionName = imageVersion.Name;
                    var imageName = keyValuePair.Value;
                    var imageVersionPublishedTimeInUtc = imageVersion.PublishingProfile.PublishedDate.Value.ToUniversalTime();
                    var isNewerThanCutoff = DateTime.Compare(imageVersionPublishedTimeInUtc, CutOffTime) > 0;
                    var isToBeRetained = index < imageIndexToBeRetained;
                    var isActiveImage = activeImageVersions.Any((activeImageVersion) => string.Equals(activeImageVersion, imageVersionName, StringComparison.OrdinalIgnoreCase));
                    var shouldDelete = !isNewerThanCutoff && !isActiveImage && !isToBeRetained;

                    childLogger
                        .FluentAddValue("ImageVersionName", imageVersionName)
                        .FluentAddValue("ImageVersionCreatedTime", imageVersionPublishedTimeInUtc)
                        .FluentAddValue("ImageVersionCutoffTime", CutOffTime)
                        .FluentAddValue("ImageVersionIsActive", isActiveImage)
                        .FluentAddValue("ImageVersionIsNewerThanCutoff", isNewerThanCutoff)
                        .FluentAddValue("ImageVersionsToKeep", imageIndexToBeRetained)
                        .FluentAddValue("ImageVersionPosition", index)
                        .FluentAddValue("ImageVersionShouldDelete", shouldDelete);

                    if (shouldDelete)
                    {
                        // Deleting the image versions that are not anymore needed.
                        await computeManagementClient
                                .GalleryImageVersions
                                .DeleteWithHttpMessagesAsync(resourceGroupName, galleryName, imageDefinitionName, imageVersionName);

                        // Deleting the images that correponds to the image versions that are deleted.
                        await computeManagementClient.Images.DeleteAsync(resourceGroupName, imageName);

                        // Slow down for rate limit & Database RUs
                        await Task.Delay(LoopDelay);
                    }
                });
        }

        private Task ProcessOrphanImageAsync(IComputeManagementClient computeManagementClient, string resourceGroupName, string galleryName, HashSet<GalleryImageInner> imageDefinitions, IDiagnosticsLogger logger)
        {
            // These are the images that dont have image versions (i,e) they are not being shared using shared image gallery.
            // Hence they dont have to be retained, if they are not active.
            return logger.OperationScopeAsync(
               $"{LogBaseName}_delete_orphan_images",
               async (childLogger) =>
               {
                   var imagesWithVersions = new List<string>();
                   foreach (var imageDefinition in imageDefinitions)
                   {
                       var lookupImageInfos = await FetchLookupImageInfo(
                           computeManagementClient,
                           resourceGroupName,
                           galleryName,
                           imageDefinition.Name);

                       imagesWithVersions.AddRange(lookupImageInfos.Values.ToArray());
                   }

                   // Fetch all the images and find out the orphan images (images that has no versions) and delete them, if they are not active.
                   var images = await computeManagementClient.Images.ListByResourceGroupAsync(resourceGroupName);
                   var imagesWithNoVersions = images
                                       .Where(x => !imagesWithVersions.Contains(x.Name))
                                       .OrderByDescending(x => x.Name).ToList();

                   int countOfImages = imagesWithNoVersions.Count();
                   for (var index = MinimumImageCountToBeRetained; index < countOfImages; index++)
                   {
                       var imageName = imagesWithNoVersions.ElementAt(index).Name;

                       childLogger
                           .FluentAddValue("ImageName", imageName)
                           .FluentAddValue("ImageHasVersion", false);

                       await computeManagementClient.Images.DeleteAsync(resourceGroupName, imageName);

                       // Slow down for rate limit
                       await Task.Delay(LoopDelay);
                   }
               });
        }

        private async Task<Dictionary<GalleryImageVersionInner, string>> FetchLookupImageInfo(IComputeManagementClient computeManagementClient, string resourceGroupName, string galleryName, string imageDefinitionName)
        {
            var lookupImageInfo = new Dictionary<GalleryImageVersionInner, string>();

            var imageVersionSubList = await computeManagementClient.GalleryImageVersions.ListByGalleryImageAsync(resourceGroupName, galleryName, imageDefinitionName);
            var nextPageLink = imageVersionSubList.NextPageLink;

            ProcessImageVersionSubList(lookupImageInfo, imageVersionSubList);

            while (nextPageLink != null)
            {
                imageVersionSubList = await computeManagementClient.GalleryImageVersions.ListByGalleryImageNextAsync(nextPageLink);
                nextPageLink = imageVersionSubList.NextPageLink;

                // Slow down for rate limit & Database RUs
                await Task.Delay(LoopDelay);

                ProcessImageVersionSubList(lookupImageInfo, imageVersionSubList);
            }

            return lookupImageInfo;
        }

        private void ProcessImageVersionSubList(Dictionary<GalleryImageVersionInner, string> lookupImageInfo, IPage<GalleryImageVersionInner> imageVersionSubList)
        {
            foreach (var obj in imageVersionSubList)
            {
                var imageName = obj.PublishingProfile.Source.ManagedImage.Id.Split('/').ToList().Last();
                lookupImageInfo.Add(obj, imageName);
            }
        }

        private async Task<IEnumerable<string>> GetActiveImageVersionsAsync(IDiagnosticsLogger logger)
        {
            var activeImages = new HashSet<string>();
            foreach (var item in SkuCatalog.BuildArtifactComputeImageFamilies.Values)
            {
                activeImages.Add(await Task.FromResult(item.GetDefaultImageVersion()));
            }

            return activeImages;
        }

        private IEnumerable<ImageFamilyType> GetArtifactTypesToCleanup()
        {
            return new List<ImageFamilyType> { ImageFamilyType.Compute, };
        }

        private Task<IDisposable> ObtainLeaseAsync(string leaseName, TimeSpan claimSpan, IDiagnosticsLogger logger)
        {
            return ClaimedDistributedLease.Obtain(
                ResourceBrokerSettings.LeaseContainerName, leaseName, claimSpan, logger);
        }
    }
}