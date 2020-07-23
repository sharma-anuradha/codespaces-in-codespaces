// <copyright file="HttpCodespacesApiClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.VsSaaS.Diagnostics;
using Microsoft.VsSaaS.Diagnostics.Extensions;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common.HttpContracts.Environments;
using Newtonsoft.Json;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.CodespacesApiClient
{
    /// <inheritdoc/>
    public class HttpCodespacesApiClient : ICodespacesApiClient
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpCodespacesApiClient"/> class.
        /// </summary>
        /// <param name="httpClient">The http client.</param>
        /// <param name="options">The http client options.</param>
        public HttpCodespacesApiClient(HttpClient httpClient, IOptions<HttpCodespacesApiClientOptions> options)
        {
            HttpClient = httpClient;

            HttpClient.BaseAddress = new Uri(options.Value.BaseAddress);

            var header = new ProductHeaderValue(options.Value.ServiceName, options.Value.Version);
            var userAgent = new ProductInfoHeaderValue(header);
            HttpClient.DefaultRequestHeaders.UserAgent.Add(userAgent);

            HttpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
            };
        }

        private HttpClient HttpClient { get; }

        /// <inheritdoc/>
        public async Task<CloudEnvironmentResult> GetCodespaceAsync(string codespaceId, IDiagnosticsLogger logger)
        {
            var duration = logger?.StartDuration();

            var fullRequestUri = new UriBuilder(HttpClient.BaseAddress)
            {
                Path = $"/api/v1/environments/{codespaceId}",
            }.Uri;

            var message = new HttpRequestMessage(HttpMethod.Get, fullRequestUri);

            message.Headers.Add("Accept", "application/json");

            try
            {
                var response = await HttpClient.SendAsync(message);
                logger?.AddClientHttpResponseDetails(response);

                await response.ThrowIfFailedAsync();

                var resultBody = await response.Content.ReadAsStringAsync();

                var environment = JsonConvert.DeserializeObject<CloudEnvironmentResult>(resultBody);

                logger?.TryAddDuration(duration);
                logger?.LogInfo("frontendwebapiclient_get_environment");

                return environment;
            }
            catch (Exception ex)
            {
                logger?.TryAddDuration(duration);
                logger?.LogException("frontendwebapiclient_get_environment_failed", ex);
            }

            return null;
        }
    }
}
