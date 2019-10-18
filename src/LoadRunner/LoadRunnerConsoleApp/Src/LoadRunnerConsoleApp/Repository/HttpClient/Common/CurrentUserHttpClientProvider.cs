// <copyright file="CurrentUserHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Providers;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
{
    /// <summary>
    /// Current User Http Client Provider.
    /// </summary>
    public class CurrentUserHttpClientProvider : ICurrentUserHttpClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserHttpClientProvider"/> class.
        /// </summary>
        /// <param name="baseAddress">Target base address.</param>
        /// <param name="currentUserProvider">Target current user provider.</param>
        public CurrentUserHttpClientProvider(
            ICurrentUserProvider currentUserProvider)
        {
            CurrentUserProvider = currentUserProvider;
        }

        /// <inheritdoc/>
        public System.Net.Http.HttpClient HttpClient { get; private set; }

        private ICurrentUserProvider CurrentUserProvider { get; }

        /// <inheritdoc/>
        public void Initalize(string serviceBaseUri)
        {
            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ForwardingBearerAuthMessageHandler(httpHandlerChain, CurrentUserProvider);
            httpHandlerChain = new ForwardingCorrelationIdHandler(httpHandlerChain);

            HttpClient = new System.Net.Http.HttpClient(httpHandlerChain)
            {
                BaseAddress = new Uri(serviceBaseUri),
            };
        }
    }
}
