// <copyright file="ManagedIdentityHttpClientProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.ManagedIdentityProvider
{
    /// <summary>
    /// Provides a singleton instance of a Azure Managed Identity HTTP Client.
    /// </summary>
    public class ManagedIdentityHttpClientProvider : IManagedIdentityHttpClientProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedIdentityHttpClientProvider"/> class.
        /// </summary>
        /// <param name="tokenBuilder">The first party token builder.</param>
        /// <param name="logger">The logger.</param>
        public ManagedIdentityHttpClientProvider(IFirstPartyTokenBuilder tokenBuilder, IDiagnosticsLogger logger)
        {
            HttpMessageHandler httpHandlerChain = new HttpClientHandler();
            httpHandlerChain = new ManagedIdentityAuthorizationDelegatingHandler(
                httpHandlerChain,
                tokenBuilder,
                logger);

            HttpClient = new HttpClient(httpHandlerChain);
        }

        /// <inheritdoc/>
        public HttpClient HttpClient { get; }
    }
}
