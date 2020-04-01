// <copyright file="ServiceConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.TokenService
{
    /// <summary>
    /// Service constants, all on one place.
    /// </summary>
    public static class ServiceConstants
    {
        /// <summary>
        /// The service name.
        /// </summary>
        public const string ServiceName = "VSSaaS-Tokens";

        /// <summary>
        /// The endpoint name.
        /// </summary>
        public const string EndpointName = "VSSaaS Tokens API v1";

        /// <summary>
        /// The service description.
        /// </summary>
        public const string ServiceDescription = "Token APIs for Visual Studio services";

        /// <summary>
        /// The current API version, for routes and swagger.
        /// </summary>
        public const string CurrentApiVersion = "v1";

        /// <summary>
        /// The default V1 route for any API controller.
        /// </summary>
        public const string ApiV1Route = "api/v1/[controller]";
    }
}
