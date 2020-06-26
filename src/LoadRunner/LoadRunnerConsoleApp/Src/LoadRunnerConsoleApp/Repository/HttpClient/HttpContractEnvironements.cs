// <copyright file="HttpContractEnvironements.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient
{
    /// <summary>
    /// HTTP contract for the environements.
    /// </summary>
    public static class HttpContractEnvironements
    {
        /// <summary>
        /// The v1 API.
        /// </summary>
        public const string ApiV1Route = "api/v1";

        /// <summary>
        /// The environments route.
        /// </summary>
        public const string FrontEndEnvironmentV1Route = ApiV1Route + "/environments";

        /// <summary>
        /// Sub path for starting a compute environment.
        /// </summary>
        public const string EnvironmentsRoute = FrontEndEnvironmentV1Route;

        /// <summary>
        /// The create environment http method.
        /// </summary>
        public static readonly HttpMethod CreateEnvironmentsMethod = HttpMethod.Post;

        /// <summary>
        /// The shutdown environment http method.
        /// </summary>
        public static readonly HttpMethod ShutdownEnvironmentsMethod = HttpMethod.Post;

        /// <summary>
        /// The resume environment http method.
        /// </summary>
        public static readonly HttpMethod ResumeEnvironmentsMethod = HttpMethod.Post;

        /// <summary>
        /// The list environments http method.
        /// </summary>
        public static readonly HttpMethod ListEnvironmentsMethod = HttpMethod.Get;

        /// <summary>
        /// The create environment http method.
        /// </summary>
        public static readonly HttpMethod GetEnvironmentsMethod = HttpMethod.Get;

        /// <summary>
        /// The deallocate http method.
        /// </summary>
        public static readonly HttpMethod DeleteEnvironmentsMethod = HttpMethod.Delete;

        /// <summary>
        /// Get the allocate uri.
        /// </summary>
        /// <returns>Uri.</returns>
        public static string GetCreateEnvironmentUri() => EnvironmentsRoute;

        /// <summary>
        /// Get the list environment uri.
        /// </summary>
        /// <returns>Uri.</returns>
        public static string GetListEnvironmentUri() => EnvironmentsRoute;

        /// <summary>
        /// Get the get environment uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetGetEnvironmentUri(Guid environmentId) => GetEnvironmentUri(environmentId);

        /// <summary>
        /// Get the get environment uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetShutdownEnvironmentUri(Guid environmentId) => $"{GetEnvironmentUri(environmentId)}/shutdown";

        /// <summary>
        /// Get the get environment uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetResumeEnvironmentUri(Guid environmentId) => $"{GetEnvironmentUri(environmentId)}/start";

        /// <summary>
        /// Get the deallocate uri.
        /// </summary>
        /// <param name="environmentId">The environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetDeleteEnvironmentUri(Guid environmentId) => GetEnvironmentUri(environmentId);

        /// <summary>
        /// Get the environment uri with id.
        /// </summary>
        /// <param name="environmentId">The  environment id token.</param>
        /// <returns>Uri.</returns>
        public static string GetEnvironmentUri(Guid environmentId) => $"{EnvironmentsRoute}/{environmentId}";
    }
}
