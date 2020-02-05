// <copyright file="IImageUrlGenerator.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Generates Azure Blobs URLs.
    /// </summary>
    public interface IImageUrlGenerator
    {
        /// <summary>
        /// Generates a full URL with read access based on the VM family name.
        /// </summary>
        /// <param name="location">Azure location of image.</param>
        /// <param name="resourceType">Type of image.</param>
        /// <param name="family">Name of the OS family.</param>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <param name="expiryTime">Amount of time for which the object should remain accessible.</param>
        /// <returns>(url, filename) or (null, null) if family not found.</returns>
        Task<(string, string)> ReadOnlyUrlByVMFamily(AzureLocation location, ResourceType resourceType, string family, IDiagnosticsLogger logger, TimeSpan expiryTime = default);

        /// <summary>
        /// Generates a full URL with read access based on the image name.
        /// </summary>
        /// <param name="location">Azure location of image.</param>
        /// <param name="resourceType">Type of image.</param>
        /// <param name="imageName">Name of image.</param>
        /// <param name="expiryTime">Amount of time for which the object should remain accessible.</param>
        /// <returns>The full url or null if imageName not found.</returns>
        Task<string> ReadOnlyUrlByImageName(AzureLocation location, ResourceType resourceType, string imageName, TimeSpan expiryTime = default);
    }
}
