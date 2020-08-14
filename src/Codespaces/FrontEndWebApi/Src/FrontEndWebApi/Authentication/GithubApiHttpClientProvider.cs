// <copyright file="GithubApiHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.FrontEndWebApi.Authentication
{
    /// <summary>
    /// Provides an HTTP client for the GitHub API.
    /// </summary>
#pragma warning disable SA1649 // File name should match first type name
    public interface IGithubApiHttpClientProvider : IHttpClientProvider
#pragma warning restore SA1649 // File name should match first type name
    {
    }

    /// <summary>
    /// Provides an HTTP client for the GitHub API.
    /// </summary>
    public class GithubApiHttpClientProvider : IGithubApiHttpClientProvider
    {
        private const string GithubApiAddress = "https://api.github.com/";
        private const string GithubApiV3MediaType = "application/vnd.github.v3+json";

        /// <summary>
        /// Initializes a new instance of the <see cref="GithubApiHttpClientProvider"/> class.
        /// </summary>
        public GithubApiHttpClientProvider()
        {
            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    ServiceConstants.ServiceName,
                    typeof(Startup).Assembly.GetName().Version!.ToString()));
            HttpClient.BaseAddress = new Uri(GithubApiAddress);
            HttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(GithubApiV3MediaType));
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
