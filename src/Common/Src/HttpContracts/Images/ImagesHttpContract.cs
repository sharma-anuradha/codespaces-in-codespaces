// <copyright file="ImagesHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.Images
{
    /// <summary>
    /// HTTP contract for the backend images controller.
    /// </summary>
    public static class ImagesHttpContract
    {
        /// <summary>
        /// The v1 API.
        /// </summary>
        public const string ApiV1Route = "api/v1";

        /// <summary>
        /// The images controller route.
        /// </summary>
        public const string ImagesControllerRoute = ApiV1Route + "/images";

        /// <summary>
        /// The route of the endpoint to get details about a specific image (name or version).
        /// </summary>
        public const string GetImageRoute = "{imageType}/{family}/{property}";

        /// <summary>
        /// The get image version http method.
        /// </summary>
        public static readonly HttpMethod GetImageHttpMethod = HttpMethod.Get;

        /// <summary>
        /// Get the uri for querying the current image version for a specific image family.
        /// </summary>
        /// <param name="imageFamilyType">The image family type.</param>
        /// <param name="family">The name of the image family.</param>
        /// <param name="property">The property to query. Should be either "name" or "version"
        /// to query the image name or image version respectively.</param>
        /// <param name="defaultValue">The default value to use if an override is not present.</param>
        /// <returns>The uri that should be called.</returns>
        public static string GetImageUri(ImageFamilyType imageFamilyType, string family, string property, string defaultValue)
        {
            return $"{ImagesControllerRoute}/{imageFamilyType.ToString().ToLowerInvariant()}/{family}/{property}?defaultValue={defaultValue}";
        }
    }
}
