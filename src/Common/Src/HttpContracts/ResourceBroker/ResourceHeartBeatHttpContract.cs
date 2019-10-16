// <copyright file="ResourceHeartBeatHttpContract.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker
{
    /// <summary>
    /// Resource HeartBeat Http Contract.
    /// </summary>
    public static class ResourceHeartBeatHttpContract
    {
        /// <summary>
        /// The v1 API.
        /// </summary>
        public const string ApiV1Route = "api/v1";

        /// <summary>
        /// The resource broker route.
        /// </summary>
        public const string HeartBeatV1Route = ApiV1Route + "/resourceheartbeat";

        /// <summary>
        /// The create resource http method.
        /// </summary>
        public static readonly HttpMethod UpdateHeartBeatMethod = HttpMethod.Post;

        /// <summary>
        /// Get Uri to update HeartBeat for a VM.
        /// </summary>
        /// <param name="resourceId">VM Resource Id.</param>
        /// <returns>Uri to update HeartBeat for a VM.</returns>
        public static string GetUpdateHeartBeatUri(Guid resourceId) => $"{HeartBeatV1Route}/{resourceId}";
    }
}
