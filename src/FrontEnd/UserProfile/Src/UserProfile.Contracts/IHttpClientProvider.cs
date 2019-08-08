// <copyright file="IHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile
{
    /// <summary>
    /// Provides a singleton instance of an <see cref="HttpClient"/>.
    /// </summary>
    public interface IHttpClientProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        HttpClient HttpClient { get; }
    }
}
