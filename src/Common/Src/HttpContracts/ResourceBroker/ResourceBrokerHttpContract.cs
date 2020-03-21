// <copyright file="ResourceBrokerHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker
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
        /// The start operation.
        /// </summary>
        public const string StartOperation = "start";

        /// <summary>
        /// The suspend operation.
        /// </summary>
        public const string SuspendOperation = "suspend";

        /// <summary>
        /// The status operation.
        /// </summary>
        public const string StatusOperation = "status";

        /// <summary>
        /// The process heartbeat operation.
        /// </summary>
        public const string ProcessHeartbeatOperation = "processheartbeat";

        /// <summary>
        /// The get resource http method.
        /// </summary>
        public static readonly HttpMethod GetResourceMethod = HttpMethod.Get;

        /// <summary>
        /// The allocate resource http method.
        /// </summary>
        public static readonly HttpMethod AllocateResourceMethod = HttpMethod.Post;

        /// <summary>
        /// The start resource http method.
        /// </summary>
        public static readonly HttpMethod StartResourceMethod = HttpMethod.Post;

        /// <summary>
        /// The suspend resource http method.
        /// </summary>
        public static readonly HttpMethod SuspendResourceMethod = HttpMethod.Post;

        /// <summary>
        /// The deallocate http method.
        /// </summary>
        public static readonly HttpMethod DeleteResourceMethod = HttpMethod.Delete;

        /// <summary>
        /// The status resource http method.
        /// </summary>
        public static readonly HttpMethod StatusResourceMethod = HttpMethod.Get;

        /// <summary>
        /// The process heartbeat http method.
        /// </summary>
        public static readonly HttpMethod ProcessHeartbeatMethod = HttpMethod.Get;

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
        /// <param name="environmentId">The environment id token.</param>
        /// <param name="action">The start action.</param>
        /// <returns>Uri.</returns>
        public static string GetStartResourceUri(Guid environmentId, StartRequestAction action) =>
            $"{ResourcesRoute}/{StartOperation}?environmentId={environmentId}&action={action}";

        /// <summary>
        /// Get the suspend uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetSuspendResourceUri(Guid environmentId) =>
            $"{ResourcesRoute}/{SuspendOperation}?environmentId={environmentId}";

        /// <summary>
        /// Get the deallocate uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteResourceUri(Guid environmentId) =>
            $"{ResourcesRoute}?environmentId={environmentId}";

        /// <summary>
        /// Get the status uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <param name="resources">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetStatusResourceUri(Guid environmentId, IEnumerable<Guid> resources) =>
            $"{ResourcesRoute}/{StatusOperation}?id={string.Join(",", resources)}&environmentId={environmentId}";

        /// <summary>
        /// Get the get resource uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <param name="resourceId">The resource id token.</param>
        /// <returns>Uri.</returns>
        public static string GetProcessHeartbeatUri(Guid environmentId, Guid resourceId) =>
            $"{ResourcesRoute}/{ProcessHeartbeatOperation}?id={resourceId}&environmentId={environmentId}";
    }
}
