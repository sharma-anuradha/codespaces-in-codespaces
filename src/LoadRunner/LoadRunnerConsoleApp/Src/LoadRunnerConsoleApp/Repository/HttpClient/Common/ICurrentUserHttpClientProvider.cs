// <copyright file="ICurrentUserHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
{
    /// <summary>
    /// A provider for the an HTTP client that uses the current user's bearer token.
    /// </summary>
    public interface ICurrentUserHttpClientProvider : IHttpClientProvider
    {
        /// <summary>
        /// Initalizes http client object with given service base uri.
        /// </summary>
        /// <param name="serviceBaseUri">Target service base uri.</param>
        void Initalize(string serviceBaseUri);
    }
}
