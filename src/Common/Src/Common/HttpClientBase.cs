// <copyright file="HttpClientBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.Contracts;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.BackEndWebApiClient
{
    /// <summary>
    /// Http client base class.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public abstract class HttpClientBase<TOptions>
        where TOptions : class, IHttpClientProviderOptions, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientBase{TOptions}"/> class.
        /// </summary>
        /// <param name="httpClientProvider">Http client provider.</param>
        public HttpClientBase(
            IHttpClientProvider<TOptions> httpClientProvider)
        {
            HttpClientProvider = Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
        }

        /// <summary>
        /// Gets the http client provider.
        /// </summary>
        protected IHttpClientProvider HttpClientProvider { get; }

        /// <summary>
        /// Sends an HTTP request and deserializes the response into the
        /// <typeparamref name="TResult"/> type.
        /// </summary>
        /// <typeparam name="TInput">The type of the input object in the request body.</typeparam>
        /// <typeparam name="TResult">The type of the output object from the response body.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="input">The input object that will be serialized as JSON for the request body.</param>
        /// <param name="logger">The logger to use to log the request/response details.</param>
        /// <returns>The deserialized object from the response body.</returns>
        protected async Task<TResult> SendAsync<TInput, TResult>(
            HttpMethod method,
            string requestUri,
            TInput input,
            IDiagnosticsLogger logger)
        {
            var rawResult = await SendRawAsync(method, requestUri, input, logger.NewChildLogger());

            try
            {
                var result = JsonConvert.DeserializeObject<TResult>(rawResult);

                return result;
            }
            catch (Exception)
            {
                logger?.LogError(GetType().FormatLogErrorMessage(nameof(SendAsync)));
                throw;
            }
        }

        /// <summary>
        /// Sends an HTTP request and returns the raw response as a string.
        /// </summary>
        /// <typeparam name="TInput">The type of the input object in the request body.</typeparam>
        /// <param name="method">The HTTP method to use for the request.</param>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="input">The input object that will be serialized as JSON for the request body.</param>
        /// <param name="logger">The logger to use to log the request/response details.</param>
        /// <returns>The response body as a string.</returns>
        protected async Task<string> SendRawAsync<TInput>(
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

            // TODO: add the correlation id header..any other interesting headers.
            httpRequestMessage.Headers.Add("Accept", "application/json");

            if (input != null)
            {
                var body = JsonConvert.SerializeObject(input);
                httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            // Send the request
            HttpResponseMessage httpResponseMessage;
            try
            {
                httpResponseMessage = await HttpClientProvider.HttpClient.SendAsync(httpRequestMessage);
                logger?.AddClientHttpResponseDetails(httpResponseMessage);

                await httpResponseMessage.ThrowIfFailedAsync();

                // Get the response body
                var resultBody = await httpResponseMessage.Content.ReadAsStringAsync();

                logger?.TryAddDuration(duration);
                logger?.LogInfo(GetType().FormatLogMessage(nameof(SendRawAsync)));

                return resultBody;
            }
            catch (Exception e)
            {
                logger?.TryAddDuration(duration);
                logger?.LogException(GetType().FormatLogErrorMessage(nameof(SendRawAsync)), e);
                throw;
            }
        }
    }
}
