// <copyright file="ImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.Azure.Management.Compute.Fluent.VirtualMachine.DefinitionUnmanaged;
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
        /// <param name="vmImageBaseName">The vm image base name.</param>
        /// <param name="vmImageSubscriptionId">The vm image subscription id.</param>
        /// <param name="vmImageResourceGroup">The vm image resource group.</param>
        public VmImageFamily(
            string imageFamilyName,
            VmImageKind imageKind,
            string imageName,
            string vmImageSubscriptionId,
            string vmImageResourceGroup)
        {
            Requires.NotNullOrEmpty(imageFamilyName, nameof(imageFamilyName));
            Requires.NotNullOrEmpty(imageName, nameof(imageName));
            if (imageKind == VmImageKind.Custom)
            {
                Requires.NotNullOrEmpty(vmImageResourceGroup, nameof(vmImageResourceGroup));
                Requires.NotNullOrEmpty(vmImageSubscriptionId, nameof(vmImageSubscriptionId));
            }

            ImageFamilyName = imageFamilyName;
            ImageName = imageName;
            ImageKind = imageKind;
            VmImageSubscriptionId = vmImageSubscriptionId;
            VmImageResourceGroup = vmImageResourceGroup;
        }

        /// <inheritdoc/>
        public string ImageFamilyName { get; }

        /// <inheritdoc/>
        public VmImageKind ImageKind { get; }

        private string ImageName { get; }

        private string VmImageSubscriptionId { get; }

        private string VmImageResourceGroup { get; }

        /// <inheritdoc/>
        public string GetCurrentImageUrl(AzureLocation location)
        {
            switch (ImageKind)
            {
                case VmImageKind.Canonical:
                    return ImageName;

                case VmImageKind.Custom:
                    return $"subscriptions/{VmImageSubscriptionId}/resourceGroups/{VmImageResourceGroup}/providers/Microsoft.Compute/images/{ImageName}.{location.ToString().ToLowerInvariant()}";

                default:
                    throw new NotSupportedException($"Image kind '{ImageKind}' is not supported.");
            }
        }
    }
}
