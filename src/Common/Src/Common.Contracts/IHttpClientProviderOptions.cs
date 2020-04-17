// <copyright file="IHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
{
    /// <summary>
    /// Base options for the http client provider.
    /// </summary>
    public interface IHttpClientProviderOptions
    {
        /// <summary>
        /// Gets or sets the HTTP client base URL.
        /// </summary>
        string BaseAddress { get; set; }
    }
}
