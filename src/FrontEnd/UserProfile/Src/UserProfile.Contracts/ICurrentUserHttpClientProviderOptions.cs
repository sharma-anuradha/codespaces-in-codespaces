// <copyright file="ICurrentUserHttpClientProviderOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Base options for the http client provider provider.
    /// </summary>
    public interface ICurrentUserHttpClientProviderOptions
    {
        /// <summary>
        /// Gets or sets the HTTP client base URL.
        /// </summary>
        string BaseAddress { get; set; }
    }
}
