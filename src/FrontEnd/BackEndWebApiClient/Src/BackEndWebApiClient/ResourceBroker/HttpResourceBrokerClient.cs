// <copyright file="HttpResourceBrokerClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
    public class HttpResourceBrokerClient : IResourceBrokerResourcesHttpContract
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
        public async Task<ResourceBrokerResource> CreateResourceAsync(CreateResourceRequestBody input, IDiagnosticsLogger logger)
        {
            var requestUri = ResourceBrokerHttpContract.GetCreateResourceUri();
            var result = await SendAsync<CreateResourceRequestBody, ResourceBrokerResource>(ResourceBrokerHttpContract.CreateResourceMethod, requestUri, input, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ResourceBrokerResource> GetResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(resourceIdToken, nameof(resourceIdToken));
            var requestUri = ResourceBrokerHttpContract.GetGetResourceUri(resourceIdToken);
            var result = await SendAsync<string, ResourceBrokerResource>(ResourceBrokerHttpContract.GetResourceMethod, requestUri, null, logger);
            return result;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteResourceAsync(string resourceIdToken, IDiagnosticsLogger logger)
        {
            Requires.NotNullOrEmpty(resourceIdToken, nameof(resourceIdToken));
            var requestUri = ResourceBrokerHttpContract.GetDeleteResourceUri(resourceIdToken);
            var result = await SendAsync<string, bool>(ResourceBrokerHttpContract.DeleteResourceMethod, requestUri, null, logger);
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
            var httpRequestMessage = new HttpRequestMessage(method, requestUri);

            // Set up logging
            var duration = logger?.StartDuration();
            var fullRequestUri = new UriBuilder(HttpClientProvider.HttpClient.BaseAddress)
            {
                Path = requestUri,
            }.Uri;
            logger?
                .FluentAddValue(LoggingConstants.HttpRequestMethod, method.ToString())
                .FluentAddValue(LoggingConstants.HttpRequestUri, fullRequestUri.ToString());

            // TODO: add the correlation id header..any other interesting headers.
            httpRequestMessage.Headers.Add("Accept", "application/json");

            var body = JsonConvert.SerializeObject(input);
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");

            // Send the request
            HttpResponseMessage httpResponseMessage;
            try
            {
                httpResponseMessage = await HttpClientProvider.HttpClient.SendAsync(httpRequestMessage);
                await httpResponseMessage.ThrowIfFailedAsync();

                // Get the response body
                var resultBody = await httpResponseMessage.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TResult>(resultBody);

                logger?.TryAddDuration(duration);
                logger?.LogInfo(GetType().FormatLogMessage(nameof(SendAsync)));

                return result;
            }
            catch (Exception)
            {
                logger?.TryAddDuration(duration);
                logger?.LogError(GetType().FormatLogErrorMessage(nameof(SendAsync)));
                throw;
            }
        }
    }
}
