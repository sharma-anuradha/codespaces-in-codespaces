// <copyright file="VmImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class VmImageFamily : IVmImageFamily
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VmImageFamily"/> class.
        /// </summary>
        /// <param name="stampInfo">An IControlPlaneStampInfo.</param>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="imageKind">The image kind.</param>
        /// <param name="defaultImageName">The default full image url.</param>
        /// <param name="defaultImageVersion">The default image version.</param>
        /// <param name="vmImageSubscriptionId">The vm image subscription id.</param>
        /// <param name="currentImageInfoProvider">The current image info provider.</param>
        public VmImageFamily(
            IControlPlaneStampInfo stampInfo,
            string imageFamilyName,
            ImageKind imageKind,
            string defaultImageName,
            string defaultImageVersion,
            string vmImageSubscriptionId,
            ICurrentImageInfoProvider currentImageInfoProvider)
        {
            Requires.NotNull(stampInfo, nameof(stampInfo));
            Requires.NotNullOrEmpty(imageFamilyName, nameof(imageFamilyName));
            Requires.NotNullOrEmpty(defaultImageName, nameof(defaultImageName));
            Requires.NotNullOrEmpty(defaultImageVersion, nameof(defaultImageVersion));

            if (imageKind == ImageKind.Custom)
            {
                Requires.NotNullOrEmpty(vmImageSubscriptionId, nameof(vmImageSubscriptionId));
            }

            Requires.NotNull(currentImageInfoProvider, nameof(currentImageInfoProvider));

            ImageFamilyName = imageFamilyName;
            ImageKind = imageKind;
            DefaultImageName = defaultImageName;
            DefaultImageVersion = defaultImageVersion;
            VmImageSubscriptionId = vmImageSubscriptionId;
            StampInfo = stampInfo;
            CurrentImageInfoProvider = currentImageInfoProvider;
        }

        /// <inheritdoc/>
        public string ImageFamilyName { get; }

        /// <inheritdoc/>
        public ImageFamilyType ImageFamilyType { get; } = ImageFamilyType.Compute;

        /// <inheritdoc/>
        public ImageKind ImageKind { get; }

        /// <inheritdoc/>
        public string DefaultImageName { get; }

        /// <inheritdoc/>
        public string DefaultImageVersion { get; }

        private string VmImageSubscriptionId { get; }

        private IControlPlaneStampInfo StampInfo { get; }

        private ICurrentImageInfoProvider CurrentImageInfoProvider { get; }

        /// <inheritdoc/>
        public async Task<string> GetCurrentImageUrlAsync(AzureLocation location, IDiagnosticsLogger logger)
        {
            var imageName = await GetCurrentImageNameAsync(logger);
            var imageVersion = await GetCurrentImageVersionAsync(logger);

            switch (ImageKind)
            {
                case ImageKind.Canonical:
                    return $"{imageName}:{imageVersion}";

                case ImageKind.Custom:
                    return $"subscriptions/{VmImageSubscriptionId}/resourceGroups/{StampInfo.GetResourceGroupNameForWindowsImages(location)}/providers/Microsoft.Compute/galleries/{StampInfo.GetImageGalleryNameForWindowsImages(location)}/images/windows/versions/{imageVersion}";

                default:
                    throw new NotSupportedException($"Image kind '{ImageKind}' is not supported.");
            }
        }

        private Task<string> GetCurrentImageNameAsync(IDiagnosticsLogger logger)
        {
            return CurrentImageInfoProvider.GetImageNameAsync(ImageFamilyType, ImageFamilyName, DefaultImageName, logger);
        }

        private Task<string> GetCurrentImageVersionAsync(IDiagnosticsLogger logger)
        {
            return CurrentImageInfoProvider.GetImageVersionAsync(ImageFamilyType, ImageFamilyName, DefaultImageVersion, logger);
        }
    }
}
