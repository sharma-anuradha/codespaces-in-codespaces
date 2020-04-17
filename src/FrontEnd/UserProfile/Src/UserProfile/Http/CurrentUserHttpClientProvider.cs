// <copyright file="CurrentUserHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http
{
    /// <inheritdoc/>
    public class CurrentUserHttpClientProvider<TOptions>
        : IHttpClientProvider<TOptions>
        where TOptions : class, IHttpClientProviderOptions, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserHttpClientProvider{TOptions}"/> class.
        /// </summary>
        /// <param name="options">The options instance.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        /// <param name="productInfo">Product info for the current service, to be added to
        /// request headers.</param>
        public CurrentUserHttpClientProvider(
            IOptions<TOptions> options,
            ICurrentUserProvider currentUserProvider,
            ProductInfoHeaderValue productInfo)
        {
            Requires.NotNull(currentUserProvider, nameof(currentUserProvider));
            Requires.NotNull(options, nameof(options));
            Requires.NotNull(productInfo, nameof(productInfo));

            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ForwardingBearerAuthMessageHandler(httpHandlerChain, currentUserProvider);
            httpHandlerChain = new ForwardingCorrelationIdHandler(httpHandlerChain);
            httpHandlerChain = new ProductInfoHeaderHandler(httpHandlerChain, productInfo);

            HttpClient = new HttpClient(httpHandlerChain)
            {
                BaseAddress = new Uri(options.Value.BaseAddress),
            };
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
