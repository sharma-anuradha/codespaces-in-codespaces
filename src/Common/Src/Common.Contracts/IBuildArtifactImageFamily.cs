// <copyright file="IBuildArtifactImageFamily.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Represents an instance of an image family.
    /// </summary>
    public interface IBuildArtifactImageFamily : IImageFamily
    {
        /// <summary>
        /// Gets the current image name that should be used for this image family.
        /// </summary>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The name of the image to use.</returns>
        Task<string> GetCurrentImageNameAsync(IDiagnosticsLogger logger);
    }
}
