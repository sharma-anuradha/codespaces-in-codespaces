// <copyright file="ResourceBrokerHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker
{
    /// <summary>
    /// HTTP contract for the resource broker.
    /// </summary>
    public static class ResourceBrokerHttpContract
    {
        /// <summary>
        /// The base uri.
        /// </summary>
        public const string BaseUriPath = "api/v1/resourcebroker";

        /// <summary>
        /// Sub path for managing resources.
        /// </summary>
        public const string ResourcesSubPath = "resources";

        /// <summary>
        /// Sub path for starting a resource.
        /// </summary>
        public const string StartComputeSubPath = ResourcesSubPath + "/start";

        /// <summary>
        /// The allocate http method.
        /// </summary>
        public static readonly HttpMethod AllocateMethod = HttpMethod.Post;

        /// <summary>
        /// The deallocate http method.
        /// </summary>
        public static readonly HttpMethod DeallocateMethod = HttpMethod.Delete;

        /// <summary>
        /// The start compute http method.
        /// </summary>
        public static readonly HttpMethod StartComputeMethod = HttpMethod.Post;

        /// <summary>
        /// Get the allocate uri.
        /// </summary>
        /// <returns>Uri.</returns>
        public static string GetAllocateUri() => $"{BaseUriPath}/{ResourcesSubPath}";

        /// <summary>
        /// Get the deallocate uri.
        /// </summary>
        /// <param name="resourceIdToken">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetDeallocateUri(string resourceIdToken) => $"{BaseUriPath}/{ResourcesSubPath}?id={resourceIdToken}";

        /// <summary>
        /// Get the start compute uri.
        /// </summary>
        /// <param name="computeResourceIdToken">The compute resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetStartComputeUri(string computeResourceIdToken) => $"{BaseUriPath}/{StartComputeSubPath}?id={computeResourceIdToken}";
    }
}
