// <copyright file="HttpResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.HttpContracts.ResourceBroker;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile.Http;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient.ResourceBroker
{
    /// <summary>
    /// An http resource broker client.
    /// </summary>
    public class HttpResourceBrokerClient : IResourceBrokerHttpContract
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
        public async Task<AllocateResponseBody> AllocateAsync(AllocateRequestBody input, IDiagnosticsLogger logger)
        {
            var requestUri = ResourceBrokerHttpContract.GetAllocateUri();
            var result = await SendAsync<AllocateRequestBody, AllocateResponseBody>(ResourceBrokerHttpContract.AllocateMethod, requestUri, input, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> DeallocateAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(resourceIdToken, nameof(resourceIdToken));
            var requestUri = ResourceBrokerHttpContract.GetDeallocateUri(resourceIdToken);
            var result = await SendAsync<string, bool>(ResourceBrokerHttpContract.DeallocateMethod, requestUri, null, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<StartComputeResponseBody> StartComputeAsync(string computeResourceIdToken, StartComputeRequestBody startComputeRequestBody, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(computeResourceIdToken, nameof(computeResourceIdToken));
            var requestUri = ResourceBrokerHttpContract.GetStartComputeUri(computeResourceIdToken);
            var result = await SendAsync<StartComputeRequestBody, StartComputeResponseBody>(
                ResourceBrokerHttpContract.StartComputeMethod,
                requestUri,
                startComputeRequestBody,
                logger);
            return result;
        }

        // TODO: Move this into a base class, or an extension method.
        private async Task<TResult> SendAsync<TInput, TResult>(
            HttpMethod method,
            string requestUri,
            TInput input,
            IDiagnosticsLogger logger)
        {
            // TODO: add logging
            _ = logger;

            //// The request message
            //var fullUri = new UriBuilder(HttpClientProvider.HttpClient.BaseAddress)
            //{
            //    Path = requestUri,
            //}.Uri;

            var httpRequestMessage = new HttpRequestMessage(method, requestUri);
            httpRequestMessage.Headers.Add("Accept", "application/json");

            var body = JsonConvert.SerializeObject(input);
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

            // TODO: add the correlation id header..any other interesting headers.

            // Send the request
            HttpResponseMessage httpResponseMessage;
            try
            {
                httpResponseMessage = await HttpClientProvider.HttpClient.SendAsync(httpRequestMessage);
                await httpResponseMessage.ThrowIfFailedAsync();
            }
            catch (Exception ex)
            {
                logger.LogException("TODO: HTPP REQUEST FAILED", ex);
                throw;
            }

            // The response body
            var resultBody = await httpResponseMessage.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<TResult>(resultBody);
            return result;
        }
    }
}
