// <copyright file="HttpClientAuthRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json.Linq;

namespace Microsoft.VsSaaS.Services.CloudEnvironments.LiveshareAuthentication
{
    /// <inheritdoc/>
    public class HttpClientAuthRepository : IAuthRepository
    {
        private const string Path = "/auth/exchange";
        private const string AuthProvider = "microsoft";

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientAuthRepository"/> class.
        /// </summary>
        /// <param name="httpClientProvider">The client provider.</param>
        public HttpClientAuthRepository(ICurrentUserHttpClientProvider<AuthHttpClientProviderOptions> httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        private IHttpClientProvider HttpClientProvider { get; }

        /// <inheritdoc/>
        public async Task<string> ExchangeToken(string externalToken)
        {
            Requires.NotNullOrWhiteSpace(externalToken, nameof(externalToken));

            var response = await HttpClientProvider.HttpClient.PostAsync(
                Path,
                new
                {
                    provider = AuthProvider,
                    token = externalToken,
                },
                new NoCharSetJsonMediaTypeFormatter(),
                CancellationToken.None);

            await response.ThrowIfFailedAsync();
            var authTokenJson = await response.Content.ReadAsAsync<JObject>();
            var authToken = authTokenJson.Value<string>("access_token");
            return authToken;
        }

        /// <summary>
        /// The auth service is implemented in NODE.JS and doesn't like the charset content type to be set.
        /// (From the liveshare agent code and documentation.)
        /// </summary>
        private class NoCharSetJsonMediaTypeFormatter : JsonMediaTypeFormatter
        {
            public override void SetDefaultContentHeaders(
                Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
            {
                base.SetDefaultContentHeaders(type, headers, mediaType);
                headers.ContentType.CharSet = string.Empty;
            }
        }
    }
}
