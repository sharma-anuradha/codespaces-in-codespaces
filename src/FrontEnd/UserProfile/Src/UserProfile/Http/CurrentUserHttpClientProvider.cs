// <copyright file="CurrentUserHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <inheritdoc/>
    public class CurrentUserHttpClientProvider<TOptions>
        : ICurrentUserHttpClientProvider<TOptions>
        where TOptions : class, ICurrentUserHttpClientProviderOptions, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserHttpClientProvider{TOptions}"/> class.
        /// </summary>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="options">The options instance.</param>
        public CurrentUserHttpClientProvider(
            ICurrentUserProvider currentUserProvider,
            IOptions<TOptions> options)
        {
            Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            Requires.NotNull(options, nameof(options));

            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ForwardingBearerAuthMessageHandler(httpHandlerChain, currentUserProvider);
            httpHandlerChain = new ForwardingCorrelationIdHandler(httpHandlerChain);

            HttpClient = new HttpClient(httpHandlerChain)
            {
                BaseAddress = new Uri(options.Value.BaseAddress),
            };

            var header = new ProductHeaderValue("VSOnlineFrontEnd", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            var userAgent = new ProductInfoHeaderValue(header);
            HttpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
