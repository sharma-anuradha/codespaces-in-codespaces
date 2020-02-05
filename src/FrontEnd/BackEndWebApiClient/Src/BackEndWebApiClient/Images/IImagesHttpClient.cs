// <copyright file="IImagesHttpClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.Images
{
    /// <summary>
    /// Http client for the backend service's images controller.
    /// </summary>
    public interface IImagesHttpClient
    {
        /// <summary>
        /// Gets the value of a property related to a specific image family.
        /// </summary>
        /// <param name="imageType">The image family type.</param>
        /// <param name="family">The image family.</param>
        /// <param name="property">The property to query. Should be either "name" or "version".</param>
        /// <param name="defaultValue">The default value to use if no override is found.</param>
        /// <param name="logger">The logger to use for this operation.</param>
        /// <returns>The value that should be used.</returns>
        Task<string> GetAsync(
            ImageFamilyType imageType,
            string family,
            string property,
            string defaultValue,
            IDiagnosticsLogger logger);
    }
}