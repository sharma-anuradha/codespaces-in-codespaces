// <copyright file="HttpClientAuthRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VsSaaS.Services.CloudEnvironments.Common;
using Microsoft.VsSaaS.Services.CloudEnvironments.LiveShareAuthentication;
using Microsoft.VsSaaS.Services.CloudEnvironments.UserProfile;
using Newtonsoft.Json;
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

            var payload = JsonConvert.SerializeObject(new
            {
                provider = AuthProvider,
                token = externalToken,
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            // The auth service is implemented in NODE.JS and doesn't like the charset content type to be set.
            // (From the liveshare agent code and documentation.)
            content.Headers.ContentType.CharSet = string.Empty;

            var response = await HttpClientProvider.HttpClient.PostAsync(Path, content);
            await response.ThrowIfFailedAsync();

            var authTokenJson = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(authTokenJson);
            var authToken = json.Value<string>("access_token");
            return authToken;
        }
    }
}
