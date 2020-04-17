// <copyright file="IHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts
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

    /// <summary>
    /// Provides a singleton instance of an <see cref="HttpClient"/>, with options.
    /// </summary>
    /// <typeparam name="TOptions">The options type for this HTTP client provider.</typeparam>
    public interface IHttpClientProvider<out TOptions> : IHttpClientProvider
        where TOptions : class, IHttpClientProviderOptions, new()
    {
    }
}
