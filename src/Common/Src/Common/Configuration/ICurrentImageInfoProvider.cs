// <copyright file="ICurrentImageInfoProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Configuration
{
    /// <summary>
    /// Provides the current information about which images should be used.
    /// Implementations can be created to allow updating the image version while
    /// the service is running, such as by having overrides from from a database or
    /// another service.
    /// </summary>
    public interface ICurrentImageInfoProvider
    {
        /// <summary>
        /// Gets the current image name that should be used for a specific image family.
        /// </summary>
        /// <param name="imageFamilyType">The type of image family that is being referenced.</param>
        /// <param name="imageFamilyName">The image family to return the current image name for.</param>
        /// <param name="defaultImageName">The default image name to use.</param
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The image name that should be used.</returns>
        Task<string> GetImageNameAsync(ImageFamilyType imageFamilyType, string imageFamilyName, string defaultImageName, IDiagnosticsLogger logger);

        /// <summary>
        /// Gets the current image version that should be used for a specific image family.
        /// </summary>
        /// <param name="imageFamilyType">The type of image family that is being referenced.</param>
        /// <param name="imageFamilyName">The image family to return the current image version for.</param>
        /// <param name="defaultImageVersion">The default image version to use.</param>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The image version that should be used.</returns>
        Task<string> GetImageVersionAsync(ImageFamilyType imageFamilyType, string imageFamilyName, string defaultImageVersion, IDiagnosticsLogger logger);
    }
}
