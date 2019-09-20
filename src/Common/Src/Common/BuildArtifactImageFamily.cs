// <copyright file="BuildArtifactImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common
{
    /// <inheritdoc/>
    public class BuildArtifactImageFamily : IBuildArtifactImageFamily
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildArtifactImageFamily"/> class.
        /// </summary>
        /// <param name="imageFamilyName">The image family name.</param>
        /// <param name="imageName">The full image url.</param>
        public BuildArtifactImageFamily(
            string imageFamilyName,
            string imageName)
        {
            Requires.NotNullOrEmpty(imageFamilyName, nameof(imageFamilyName));
            Requires.NotNullOrEmpty(imageName, nameof(imageName));

            ImageFamilyName = imageFamilyName;
            ImageName = imageName;
        }

        /// <inheritdoc/>
        public string ImageFamilyName { get; }

        /// <inheritdoc/>
        public string ImageName { get; }
    }
}
