// <copyright file="CreateCloudEnvironmentResult.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Class which encapsulates the results of cloud environment creation.
    /// </summary>
    public class CreateCloudEnvironmentResult
    {
        /// <summary>
        /// Gets or sets the Cloud Environment object.
        /// </summary>
        public CloudEnvironment CloudEnvironment { get; set; }

        /// <summary>
        /// Gets or sets the http status code.
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Gets or sets the error code for the user/client.
        /// </summary>
        public string ErrorCode { get; set; }
    }
}
