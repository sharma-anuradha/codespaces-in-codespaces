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
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LoadRunnerConsoleApp.Repository.HttpClient.Common
{
    /// <summary>
    /// Http client base class.
    /// </summary>
    public abstract class HttpClientBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientBase"/> class.
        /// </summary>
        /// <param name="httpClientProvider">Http client provider.</param>
        public HttpClientBase(
            ICurrentUserHttpClientProvider httpClientProvider)
        {
            HttpClientProvider = Requires.NotNull(httpClientProvider, nameof(httpClientProvider));
        }

        /// <summary>
        /// Gets the logging name that should be used.
        /// </summary>
        protected abstract string LogBaseName { get; }

        /// <summary>
        /// Gets of sets the http client provider.
        /// </summary>
        protected IHttpClientProvider HttpClientProvider { get; }

        /// <summary>
        /// Sends request using current http client.
        /// </summary>
        /// <typeparam name="TInput">Input type.</typeparam>
        /// <typeparam name="TResult">Result type.</typeparam>
        /// <param name="method">Target method.</param>
        /// <param name="requestUri">Target request uri.</param>
        /// <param name="input">Target input.</param>
        /// <param name="logger">Target logger.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        protected async Task<TResult> SendAsync<TInput, TResult>(
            HttpMethod method,
            string requestUri,
            TInput input,
            IDiagnosticsLogger logger)
        {
            var rawResult = await SendRawAsync(method, requestUri, input, logger);

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
