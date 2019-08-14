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
        /// The v1 API.
        /// </summary>
        public const string ApiV1Route = "api/v1/";

        /// <summary>
        /// The resource broker route.
        /// </summary>
        public const string ResourceBrokerV1Route = ApiV1Route + "resourcebroker/";

        /// <summary>
        /// The resource broker resources route.
        /// </summary>
        public const string ResourcesRoute = ResourceBrokerV1Route + "resources/";

        /// <summary>
        /// The resource start operation route.
        /// </summary>
        public const string StartComputeOperation = "start";

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod CreateResourceMethod = HttpMethod.Post;

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod GetResourceMethod = HttpMethod.Get;

        /// <summary>
        /// The deallocate http method.
        /// </summary>
        public static readonly HttpMethod DeleteResourceMethod = HttpMethod.Delete;

        /// <summary>
        /// The start compute http method.
        /// </summary>
        public static readonly HttpMethod StartComputeMethod = HttpMethod.Post;

        /// <summary>
        /// Get the create resource uri.
        /// </summary>
        /// <returns>Uri.</returns>
        public static string GetCreateResourceUri() => ResourcesRoute;

        /// <summary>
        /// Get the get resoruce uri.
        /// </summary>
        /// <param name="resourceIdToken">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetGetResourceUri(string resourceIdToken) => GetResourceUri(resourceIdToken);

        /// <summary>
        /// Get the deallocate uri.
        /// </summary>
        /// <param name="resourceIdToken">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteResourceUri(string resourceIdToken) => GetResourceUri(resourceIdToken);

        /// <summary>
        /// Get the start compute operation uri.
        /// </summary>
        /// <param name="computeResourceIdToken">The compute resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetStartComputeUri(string computeResourceIdToken) => GetResourceOperationUri(computeResourceIdToken, StartComputeOperation);

        private static string GetResourceUri(string resourceIdToken) => $"{ResourcesRoute}?id={resourceIdToken}";

        private static string GetResourceOperationUri(string resourceIdToken, string operationSubRoute) => $"{ResourcesRoute}/{operationSubRoute}?id={resourceIdToken}";
    }
}
