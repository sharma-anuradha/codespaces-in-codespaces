// <copyright file="IVmImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Common;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents an instance of an image family.
    /// </summary>
    public interface IVmImageFamily : IImageFamily
    {
        /// <summary>
        /// Gets the image OS.
        /// </summary>
        ImageKind ImageKind { get; }

        /// <summary>
        /// Gets the default image name for this family.
        /// </summary>
        string DefaultImageName { get; }

        /// <summary>
        /// Gets the default image version for this family.
        /// </summary>
        string DefaultImageVersion { get; }

        /// <summary>
        /// Gets the VS channel url associated with the image.
        /// </summary>
        string VsChannelUrl { get; }

        /// <summary>
        /// Gets the VS version installed in the image.
        /// </summary>
        string VsVersion { get; }

        /// <summary>
        /// Gets the image URL for the specified location.
        /// </summary>
        /// <param name="location">The azure location.</param>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The image url.</returns>
        Task<string> GetCurrentImageUrlAsync(AzureLocation location, IDiagnosticsLogger logger);
    }
}
