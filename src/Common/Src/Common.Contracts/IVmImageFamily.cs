// <copyright file="IVmImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Microsoft.VsSaaS.Common;

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
        VmImageKind ImageKind { get; }

        /// <summary>
        /// Gets the image URL for the specified location.
        /// </summary>
        /// <param name="location">The azure location.</param>
        /// <returns>The image url.</returns>
        string GetCurrentImageUrl(AzureLocation location);
    }
}
