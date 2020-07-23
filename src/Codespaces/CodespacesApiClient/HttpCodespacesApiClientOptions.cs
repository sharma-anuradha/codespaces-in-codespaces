// <copyright file="HttpCodespacesApiClientOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient
{
    /// <summary>
    /// Front end client configuration options.
    /// </summary>
    public class HttpCodespacesApiClientOptions
    {
        /// <summary>
        /// Gets or sets the HTTP client base URL.
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        /// Gets or sets the client name.
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets the client version.
        /// </summary>
        public string Version { get; set; }
    }
}