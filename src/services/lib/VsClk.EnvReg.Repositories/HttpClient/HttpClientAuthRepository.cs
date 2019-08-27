using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;
using VsClk.EnvReg.Repositories.Support.HttpClient;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System;

namespace VsClk.EnvReg.Repositories.HttpClient
{
    public class HttpClientAuthRepository : IAuthRepository
    {
        const string Path = "/auth/exchange";
        const string AuthProvider = "microsoft";

        private IHttpClientProvider HttpClientProvider { get; }

        public HttpClientAuthRepository(IHttpClientProvider httpClientProvider)
        {
            HttpClientProvider = httpClientProvider;
        }

        public async Task<string> ExchangeToken(string externalToken)
        {
            var response = await this.HttpClientProvider.AuthServiceClient.PostAsync(
                Path,
                new
                {
                    provider = AuthProvider,
                    token = externalToken
                },
                new NoCharSetJsonMediaTypeFormatter(), CancellationToken.None);

            await response.ThrowIfFailedAsync();
            var authTokenJson = await response.Content.ReadAsAsync<JObject>();
            var authToken = authTokenJson.Value<string>("access_token");
            return authToken;
        }

        private class NoCharSetJsonMediaTypeFormatter : JsonMediaTypeFormatter
        {
            public override void SetDefaultContentHeaders(
                Type type, HttpContentHeaders headers, MediaTypeHeaderValue mediaType)
            {
                base.SetDefaultContentHeaders(type, headers, mediaType);
                headers.ContentType.CharSet = "";
            }
        }
    }
}
