// <copyright file="ServiceConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.PortForwarding.ErrorsBackend
{
    /// <summary>
    /// Service constants, all on one place.
    /// </summary>
    public static class ServiceConstants
    {
        /// <summary>
        /// The service name.
        /// </summary>
        public const string ServiceName = "ErrorsBackend";

        /// <summary>
        /// The endpoint name.
        /// </summary>
        public const string EndpointName = "Default nginx backend for port forwarding.";

        /// <summary>
        /// The service description.
        /// </summary>
        public const string ServiceDescription = "Default nginx backend for port forwarding.";

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
