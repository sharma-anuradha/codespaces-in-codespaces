// <copyright file="BuildArtifactImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class BuildArtifactImageFamily : IBuildArtifactImageFamily
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildArtifactImageFamily"/> class.
        /// </summary>
        /// <param name="imageFamilyType">The type of image family that is being referenced.</param>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="defaultImageName">The default full image url.</param>
        /// <param name="defaultImageVersion">The default full image version.</param>
        /// <param name="currentImageInfoProvider">The current image info provider.</param>
        public BuildArtifactImageFamily(
            ImageFamilyType imageFamilyType,
            string imageFamilyName,
            string defaultImageName,
            string defaultImageVersion,
            ICurrentImageInfoProvider currentImageInfoProvider)
        {
            Requires.NotNullOrEmpty(imageFamilyName, nameof(imageFamilyName));
            Requires.NotNullOrEmpty(defaultImageName, nameof(defaultImageName));
            Requires.NotNull(currentImageInfoProvider, nameof(currentImageInfoProvider));

            ImageFamilyType = imageFamilyType;
            ImageFamilyName = imageFamilyName;
            DefaultImageName = defaultImageName;
            DefaultImageVersion = defaultImageVersion;
            CurrentImageInfoProvider = currentImageInfoProvider;
        }

        /// <inheritdoc/>
        public ImageFamilyType ImageFamilyType { get; }

        /// <inheritdoc/>
        public string ImageFamilyName { get; }

        /// <summary>
        /// Gets the image version.
        /// </summary>
        public string DefaultImageVersion { get; }

        private string DefaultImageName { get; }

        private ICurrentImageInfoProvider CurrentImageInfoProvider { get; }

        /// <inheritdoc/>
        public Task<string> GetCurrentImageNameAsync(IDiagnosticsLogger logger)
        {
            return CurrentImageInfoProvider.GetImageNameAsync(ImageFamilyType, ImageFamilyName, DefaultImageName, logger);
        }

        /// <inheritdoc/>
        public string GetDefaultImageVersion()
        {
            return DefaultImageVersion;
        }
    }
}
