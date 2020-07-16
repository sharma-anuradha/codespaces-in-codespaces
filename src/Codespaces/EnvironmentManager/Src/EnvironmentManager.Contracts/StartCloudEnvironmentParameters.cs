// <copyright file="StartCloudEnvironmentParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Parameters required to start the compute of a CloudEnvironment.
    /// </summary>
    public class StartCloudEnvironmentParameters
    {
        /// <summary>
        /// Gets or sets the user claims to store in the environment connection token.
        /// </summary>
        public Profile UserProfile { get; set; }

        /// <summary>
        /// Gets or sets the callback uri format.
        /// </summary>
        public string CallbackUriFormat { get; set; }

        /// <summary>
        /// Gets or sets the service uri.
        /// </summary>
        public Uri FrontEndServiceUri { get; set; }

        /// <summary>
        /// Gets or sets the connection service (Live Share) uri.
        /// </summary>
        public Uri ConnectionServiceUri { get; set; }

        /// <summary>
        /// Gets or sets the user token.
        /// </summary>
        public string UserAuthToken { get; set; }
    }
}
