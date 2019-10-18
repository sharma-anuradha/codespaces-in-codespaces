// <copyright file="IHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
{
    /// <summary>
    /// Provides a singleton instance of an <see cref="HttpClient"/>.
    /// </summary>
    public interface IHttpClientProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        System.Net.Http.HttpClient HttpClient { get; }
    }
}