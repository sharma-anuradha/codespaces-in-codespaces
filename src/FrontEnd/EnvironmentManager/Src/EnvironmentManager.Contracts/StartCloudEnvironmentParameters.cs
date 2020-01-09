// <copyright file="StartCloudEnvironmentParameters.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.EnvironmentManager.Contracts
{
    /// <summary>
    /// Parameters required to start the compute of a CloudEnvironment.
    /// </summary>
    public class StartCloudEnvironmentParameters
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the callback uri format.
        /// </summary>
        public string CallbackUriFormat { get; set; }

        /// <summary>
        /// Gets or sets the service uri.
        /// </summary>
        public Uri ServiceUri { get; set; }
    }
}
