// <copyright file="VmImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class VmImageFamily : IVmImageFamily
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VmImageFamily"/> class.
        /// </summary>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="imageKind">The image kind.</param>
        /// <param name="imageName">The full image url.</param>
        /// <param name="imageVersion">The image version.</param>
        /// <param name="vmImageSubscriptionId">The vm image subscription id.</param>
        /// <param name="vmImageResourceGroupPrefix">The prefix for the vm image resource group name.</param>
        public VmImageFamily(
            string imageFamilyName,
            VmImageKind imageKind,
            string imageName,
            string imageVersion,
            string vmImageSubscriptionId,
            string vmImageResourceGroupPrefix)
        {
            Requires.NotNullOrEmpty(imageFamilyName, nameof(imageFamilyName));
            Requires.NotNullOrEmpty(imageName, nameof(imageName));
            Requires.NotNullOrEmpty(imageVersion, nameof(imageVersion));
            if (imageKind == VmImageKind.Custom)
            {
                Requires.NotNullOrEmpty(vmImageResourceGroupPrefix, nameof(vmImageResourceGroupPrefix));
                Requires.NotNullOrEmpty(vmImageSubscriptionId, nameof(vmImageSubscriptionId));
            }

            ImageFamilyName = imageFamilyName;
            ImageKind = imageKind;
            ImageName = imageName;
            ImageVersion = imageVersion;
            VmImageSubscriptionId = vmImageSubscriptionId;
            VmImageResourceGroupPrefix = vmImageResourceGroupPrefix;
        }

        /// <inheritdoc/>
        public string ImageFamilyName { get; }

        /// <inheritdoc/>
        public VmImageKind ImageKind { get; }

        private string ImageName { get; }

        private string ImageVersion { get; }

        private string VmImageSubscriptionId { get; }

        private string VmImageResourceGroupPrefix { get; }

        /// <inheritdoc/>
        public string GetCurrentImageUrl(AzureLocation location)
        {
            switch (ImageKind)
            {
                case VmImageKind.Canonical:
                    return $"{ImageName}:{ImageVersion}";

                case VmImageKind.Custom:
                    var imageResourceGroup = $"{VmImageResourceGroupPrefix}-images-{location.ToString().ToLowerInvariant()}";
                    var imageResourcePrefix = imageResourceGroup.Replace("-", "_");
                    var imageGallery = $"{imageResourcePrefix}_gallery";
                    var imageDefinition = $"{imageResourcePrefix}_imagedef";
                    return $"subscriptions/{VmImageSubscriptionId}/resourceGroups/{imageResourceGroup}/providers/Microsoft.Compute/galleries/{imageGallery}/images/{imageDefinition}/versions/{ImageVersion}";

                default:
                    throw new NotSupportedException($"Image kind '{ImageKind}' is not supported.");
            }
        }
    }
}
