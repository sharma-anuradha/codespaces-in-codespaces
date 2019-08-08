// <copyright file="ServiceConstants.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi
{
    /// <summary>
    /// Service constants, all on one place.
    /// </summary>
    /// <remarks>
    /// Nice to see all of these in once place :)
    /// .
    /// </remarks>
    public static class ServiceConstants
    {
        /// <summary>
        /// The service name.
        /// </summary>
        public const string ServiceName = "CloudEnvironments";

        /// <summary>
        /// The endput name.
        /// </summary>
        public const string EndpointName = "CloudEnvironments API v1";

        /// <summary>
        /// The service description.
        /// </summary>
        public const string ServiceDescription = "Public APIs for managing Cloud Environments";

        /// <summary>
        /// The current API version, for routes and swagger.
        /// </summary>
        public const string CurrentApiVersion = "v1";

        /// <summary>
        /// The API route for the environments controller.
        /// </summary>
        public const string EnvironmentsV1Route = "api/v1/environments";

        /// <summary>
        /// The environment variable that is set when we are running in a deployment in Azure.
        /// This is set by our deployment configuration.
        /// </summary>
        public const string RunningInAzureEnvironmentVariable = "RUNNING_IN_AZURE";

        /// <summary>
        /// The environment variable value that is expected if we are running in Azure.
        /// </summary>
        public const string RunningInAzureEnvironmentVariableValue = "true";
    }
}
