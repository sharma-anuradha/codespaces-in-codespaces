// <copyright file="TokenServiceHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.AspNetCore;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.Auth
{
    /// <summary>
    /// Options for the token service HTTP client provider.
    /// </summary>
    public class TokenServiceHttpClientProvider
        : IHttpClientProvider<TokenServiceHttpClientProviderOptions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenServiceHttpClientProvider"/> class.
        /// </summary>
        /// <param name="options">The user profile options.</param>
        /// <param name="productInfo">Product info for the current service, to be added to
        /// request headers.</param>
        public TokenServiceHttpClientProvider(
            IOptions<TokenServiceHttpClientProviderOptions> options,
            ProductInfoHeaderValue productInfo)
        {
            Requires.NotNull(options, nameof(options));
            Requires.NotNull(productInfo, nameof(productInfo));

            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
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
