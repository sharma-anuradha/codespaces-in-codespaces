// <copyright file="HttpResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// An http resource broker client.
    /// </summary>
    public class HttpResourceBrokerClient : IResourceBrokerClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpResourceBrokerClient"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The backend http client provider.</param>
        public HttpResourceBrokerClient(
            ICurrentUserHttpClientProvider<BackEndHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public Task<AllocateResult> AllocateAsync(AllocateInput input, IDiagnosticsLogger logger)
        {
            _ = HttpClientProvider.HttpClient;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<bool> DeallocateAsync(string resourceIdToken)
        {
            _ = HttpClientProvider.HttpClient;
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<BindResult> BindComputeToStorage(BindInput input)
        {
            _ = input;
            throw new NotImplementedException();
        }
    }
}
