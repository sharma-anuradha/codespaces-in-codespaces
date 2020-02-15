// <copyright file="ResourceBrokerHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
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
        public const string ApiV1Route = "api/v1";

        /// <summary>
        /// The resource broker route.
        /// </summary>
        public const string ResourceBrokerV1Route = ApiV1Route + "/resourcebroker";

        /// <summary>
        /// Sub path for starting a compute resource.
        /// </summary>
        public const string ResourcesRoute = ResourceBrokerV1Route + "/resources";

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod PostResourceMethod = HttpMethod.Post;

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod GetResourceMethod = HttpMethod.Get;

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod TriggerEnvironmentHeartbeatMethod = HttpMethod.Get;

        /// <summary>
        /// The deallocate http method.
        /// </summary>
        public static readonly HttpMethod DeleteResourceMethod = HttpMethod.Delete;

        /// <summary>
        /// The start compute http method.
        /// </summary>
        public static readonly HttpMethod StartComputeMethod = HttpMethod.Post;

        /// <summary>
        /// The get compute status http method.
        /// </summary>
        public static readonly HttpMethod GetComputeStatusMethod = HttpMethod.Get;

        /// <summary>
        /// Get the get resource uri.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetGetResourceUri(Guid resourceId) =>
            $"{ResourcesRoute}?id={resourceId}";

        /// <summary>
        /// Get the allocate uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetAllocateResourceUri(Guid environmentId) =>
            $"{ResourcesRoute}?environmentId={environmentId}";

        /// <summary>
        /// Get the start compute operation uri.
        /// </summary>
        /// <param name="computeResourceId">The compute resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetStartResourceUri(Guid computeResourceId) =>
            $"{ResourcesRoute}/obsoletestart?id={computeResourceId}";

        /// <summary>
        /// Get the suspend uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetSuspendResourceUri(Guid environmentId) =>
            $"{ResourcesRoute}/obsoletesuspend?environmentId={environmentId}";

        /// <summary>
        /// Get the deallocate uri.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteResourceUri(Guid resourceId) =>
            $"{ResourcesRoute}/obsoletedelete?id={resourceId}";

        /// <summary>
        /// Get the get resource uri.
        /// </summary>
        /// <param name="resourceId">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetProcessHeartbeatUri(Guid resourceId) =>
            $"{ResourcesRoute}/obsoleteenvironmentheartbeat?id={resourceId}";
    }
}
